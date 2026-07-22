using FluentAssertions;
using LvApplication.DTOs.Incidents;
using LvApplication.DTOs.Payroll;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.SiteLogs;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Suppliers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Projects;

public class ProjectChapterServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(AppDbContext context)
    {
        var customer = new Customer { Name = "Project Customer", CustomerType = CustomerType.Project, Status = ActiveStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId)
    {
        var branch = new Branch { Name = "Test Branch", City = "San Jose", Province = "San Jose", Status = BranchStatus.Active, BranchType = BranchType.Office, OperationsDirectorId = operationsDirectorId, CreatedAt = DateTime.UtcNow };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Supplier> CreateSupplierAsync(AppDbContext context)
    {
        var supplier = new Supplier { Name = "Proveedor Test", Status = ActiveStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    private static async Task<MaterialCatalog> CreateMaterialAsync(AppDbContext context)
    {
        var material = new MaterialCatalog { Name = "Cemento", CreatedAt = DateTime.UtcNow };
        context.MaterialCatalogs.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private static async Task<(ProjectDto Project, BudgetChapter Chapter, int ManagerId, int ProjectAdminId)> CreateProjectWithChapterAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var projectAdmin = await TestUserFactory.CreateAsync(context, $"pa-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);

        var budget = new Budget
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            Name = "Edificio Test",
            Status = BudgetStatus.ClientApproved,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var chapter = new BudgetChapter { BudgetId = budget.Id, Name = "Cimentación", Order = 1, TotalChapter = 1000m, EstimatedWeeks = 10, CreatedAt = DateTime.UtcNow };
        context.BudgetChapters.Add(chapter);
        await context.SaveChangesAsync();

        var offer = new Offer
        {
            BudgetId = budget.Id,
            CustomerId = customer.Id,
            OfferNumber = $"OF-TEST-{Guid.NewGuid():N}",
            OfferType = OfferType.Turnkey,
            IssueDate = new DateTime(2026, 1, 10),
            ValidityDays = 30,
            WorkLocation = "San Jose Centro",
            WorkScope = "Construccion de edificio de 3 niveles",
            EstimatedStartDate = new DateTime(2026, 2, 1),
            EstimatedDurationWeeks = 10,
            EstimatedDeliveryDate = new DateTime(2026, 2, 1).AddDays(10 * 7),
            PaymentTerms = "50% inicio, 50% entrega",
            Warranties = "1 año estructural",
            Exclusions = "No incluye mobiliario",
            TotalProjectPrice = 100000m,
            Status = OfferStatus.ClientAccepted,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var projectService = ServiceFactory.CreateProjectService(context);
        var project = await projectService.CreateProjectAsync(new CreateProjectDto
        {
            OfferId = offer.Id,
            BranchId = branch.Id,
            StartDate = new DateTime(2026, 3, 1)
        }, manager.Id);

        return (project, chapter, manager.Id, projectAdmin.Id);
    }

    [Fact]
    public async Task RecalculateActualCostAsync_SumsAllThreeSourcesFilteredByChapter_ExcludingSiteLog()
    {
        using var context = TestDbContextFactory.Create();
        var (project, chapter, managerId, projectAdminId) = await CreateProjectWithChapterAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);

        var ticketService = ServiceFactory.CreateMaterialTicketService(context);
        var ticket = await ticketService.CreateAsync(project.Id, new LvApplication.DTOs.Inventory.CreateMaterialTicketDto
        {
            SupplierId = supplier.Id,
            MaterialId = material.Id,
            Quantity = 10,
            UnitPrice = 5,
            ChapterId = chapter.Id
        }, projectAdminId); // Total = 50
        await ticketService.ApplyAsync(ticket.Id);

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        var siteLog = await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 2),
            WeekEnd = new DateTime(2026, 3, 8),
            TaskDescription = "Semana de trabajo",
            ChapterId = chapter.Id
        }, projectAdminId);
        await siteLogService.SubmitToReviewAsync(siteLog.Id);
        await siteLogService.ApproveAsync(siteLog.Id, managerId); // TotalMaterials should NOT be counted

        var payrollService = ServiceFactory.CreatePayrollService(context);
        var payroll = await payrollService.CreateAsync(new CreatePayrollDto
        {
            SiteLogId = siteLog.Id,
            ChapterId = chapter.Id,
            Details = new()
        }, projectAdminId); // TotalPayroll = 0, but still exercises the chapter-filtered sum path
        await payrollService.MarkAsPaidAsync(payroll.Id);

        var incidentService = ServiceFactory.CreateIncidentService(context);
        var incident = await incidentService.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 5),
            Description = "Imprevisto de prueba",
            ChapterId = chapter.Id,
            Materials = new(),
            Workers = new()
        }, projectAdminId); // TotalCost = 0
        await incidentService.ApproveAsync(incident.Id, managerId);

        var projectChapter = await context.ProjectChapters.FirstAsync(pc => pc.ProjectId == project.Id && pc.ChapterId == chapter.Id);

        projectChapter.ActualCostTotal.Should().Be(50m); // only the ticket contributes a non-zero amount
        projectChapter.ChapterProfit.Should().Be(projectChapter.AssignedSoldTotal - 50m);
        projectChapter.IncidentCount.Should().Be(1);
        projectChapter.IncidentPercentage.Should().Be(0m); // 0 / 50 * 100
    }

    [Fact]
    public async Task UpdateAssignedSoldTotalAsync_UpdatesValueAndRecalculatesChapterProfit()
    {
        using var context = TestDbContextFactory.Create();
        var (project, chapter, _, _) = await CreateProjectWithChapterAsync(context);
        var service = ServiceFactory.CreateProjectChapterService(context);

        var updated = await service.UpdateAssignedSoldTotalAsync(project.Id, chapter.Id, 12345m);

        updated.AssignedSoldTotal.Should().Be(12345m);
        updated.ChapterProfit.Should().Be(12345m); // ActualCostTotal is still 0 at this point
    }
}
