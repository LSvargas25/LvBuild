using FluentAssertions;
using LvApplication.DTOs.Inventory;
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

namespace LvTest.Services.Finance;

public class ProjectFinanceServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(AppDbContext context)
    {
        var customer = new Customer
        {
            Name = "Project Customer",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(
        AppDbContext context,
        int operationsDirectorId
    )
    {
        var branch = new Branch
        {
            Name = "Test Branch",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Supplier> CreateSupplierAsync(
        AppDbContext context,
        string name = "Proveedor Test"
    )
    {
        var supplier = new Supplier
        {
            Name = name,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    private static async Task<MaterialCatalog> CreateMaterialAsync(
        AppDbContext context,
        string name = "Cemento"
    )
    {
        var material = new MaterialCatalog { Name = name, CreatedAt = DateTime.UtcNow };
        context.MaterialCatalogs.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private static async Task<(
        ProjectDto Project,
        int ManagerId,
        int ProjectAdminId
    )> CreateActiveProjectAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(
            context,
            $"director-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var manager = await TestUserFactory.CreateAsync(
            context,
            $"gm-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var projectAdmin = await TestUserFactory.CreateAsync(
            context,
            $"pa-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.ProjectAdminRoleId
        );
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
            CreatedAt = DateTime.UtcNow,
        };
        context.Budgets.Add(budget);
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
            CreatedAt = DateTime.UtcNow,
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var projectService = ServiceFactory.CreateProjectService(context);
        var project = await projectService.CreateProjectAsync(
            new CreateProjectDto
            {
                OfferId = offer.Id,
                BranchId = branch.Id,
                StartDate = new DateTime(2026, 3, 1),
            },
            manager.Id
        );

        return (project, manager.Id, projectAdmin.Id);
    }

    [Fact]
    public async Task GetFinanceAsync_Week_AggregatesMaterialsAndHours()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context, "Ferretería Central");
        var material = await CreateMaterialAsync(context, "Cemento");

        var ticketService = ServiceFactory.CreateMaterialTicketService(context);
        var ticket = await ticketService.CreateAsync(
            project.Id,
            new CreateMaterialTicketDto
            {
                SupplierId = supplier.Id,
                MaterialId = material.Id,
                Quantity = 10,
                UnitPrice = 5,
            },
            projectAdminId
        ); // Total = 50
        await ticketService.ApplyAsync(ticket.Id);

        // MaterialTicket.CreatedAt is stamped with the real DateTime.UtcNow at creation time
        // (MaterialTicketService has no "as of" date parameter) — backdate it here so it falls
        // inside the fixed test week below.
        var storedTicket = await context.MaterialTickets.FindAsync(ticket.Id);
        storedTicket!.CreatedAt = new DateTime(2026, 3, 3);
        await context.SaveChangesAsync();

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        await siteLogService.CreateAsync(
            new CreateSiteLogDto
            {
                ProjectId = project.Id,
                WeekStart = new DateTime(2026, 3, 2),
                WeekEnd = new DateTime(2026, 3, 8),
                TaskDescription = "Semana de trabajo",
                Workers = new List<SiteLogWorkerDto>
                {
                    new() { WorkerId = 1, HoursWorked = 40 },
                },
            },
            projectAdminId
        );
        // WorkerId=1 with no Worker row is fine here — SiteLog doesn't validate Worker existence
        // (matching the pre-existing SyncWorkers behavior, unchanged by this plan).

        var service = ServiceFactory.CreateProjectFinanceService(context);

        var finance = await service.GetFinanceAsync(
            project.Id,
            FinancePeriod.Week,
            new DateTime(2026, 3, 2)
        );

        finance.PeriodStart.Should().Be(new DateTime(2026, 3, 2));
        finance.PeriodEnd.Should().Be(new DateTime(2026, 3, 8));
        finance
            .Materials.Should()
            .ContainSingle(m =>
                m.MaterialName == "Cemento"
                && m.SupplierName == "Ferretería Central"
                && m.Total == 50m
            );
        finance.TotalHoursWorked.Should().Be(40m);
    }

    [Fact]
    public async Task GetFinanceAsync_Month_AggregatesAcrossMultipleWeeks()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        await siteLogService.CreateAsync(
            new CreateSiteLogDto
            {
                ProjectId = project.Id,
                WeekStart = new DateTime(2026, 3, 2),
                WeekEnd = new DateTime(2026, 3, 8),
                TaskDescription = "Semana 1",
                Workers = new List<SiteLogWorkerDto>
                {
                    new() { WorkerId = 1, HoursWorked = 40 },
                },
            },
            projectAdminId
        );
        await siteLogService.CreateAsync(
            new CreateSiteLogDto
            {
                ProjectId = project.Id,
                WeekStart = new DateTime(2026, 3, 9),
                WeekEnd = new DateTime(2026, 3, 15),
                TaskDescription = "Semana 2",
                Workers = new List<SiteLogWorkerDto>
                {
                    new() { WorkerId = 1, HoursWorked = 35 },
                },
            },
            projectAdminId
        );

        var service = ServiceFactory.CreateProjectFinanceService(context);

        var finance = await service.GetFinanceAsync(
            project.Id,
            FinancePeriod.Month,
            new DateTime(2026, 3, 15)
        );

        finance.PeriodStart.Should().Be(new DateTime(2026, 3, 1));
        finance.PeriodEnd.Should().Be(new DateTime(2026, 3, 31));
        finance.TotalHoursWorked.Should().Be(75m);
    }

    [Fact]
    public async Task GetFinanceAsync_Year_AggregatesAcrossMultipleMonths()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        await siteLogService.CreateAsync(
            new CreateSiteLogDto
            {
                ProjectId = project.Id,
                WeekStart = new DateTime(2026, 3, 2),
                WeekEnd = new DateTime(2026, 3, 8),
                TaskDescription = "Semana de marzo",
                Workers = new List<SiteLogWorkerDto>
                {
                    new() { WorkerId = 1, HoursWorked = 40 },
                },
            },
            projectAdminId
        );
        await siteLogService.CreateAsync(
            new CreateSiteLogDto
            {
                ProjectId = project.Id,
                WeekStart = new DateTime(2026, 9, 7),
                WeekEnd = new DateTime(2026, 9, 13),
                TaskDescription = "Semana de septiembre",
                Workers = new List<SiteLogWorkerDto>
                {
                    new() { WorkerId = 1, HoursWorked = 20 },
                },
            },
            projectAdminId
        );

        var service = ServiceFactory.CreateProjectFinanceService(context);

        var finance = await service.GetFinanceAsync(
            project.Id,
            FinancePeriod.Year,
            new DateTime(2026, 6, 1)
        );

        finance.PeriodStart.Should().Be(new DateTime(2026, 1, 1));
        finance.PeriodEnd.Should().Be(new DateTime(2026, 12, 31));
        finance.TotalHoursWorked.Should().Be(60m);
    }
}
