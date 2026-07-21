using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.SiteLogs;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Inventory;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Workers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.SiteLogs;

public class SiteLogServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(AppDbContext context)
    {
        var customer = new Customer
        {
            Name = "Project Customer",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId)
    {
        var branch = new Branch
        {
            Name = "Test Branch",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Budget> CreateApprovedBudgetAsync(AppDbContext context, int customerId, int branchId, int createdByUserId)
    {
        var budget = new Budget
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = "Edificio Test",
            Status = BudgetStatus.ClientApproved,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    private static async Task<Offer> CreateAcceptedOfferAsync(AppDbContext context, int budgetId, int customerId, int createdByUserId)
    {
        var offer = new Offer
        {
            BudgetId = budgetId,
            CustomerId = customerId,
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
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();
        return offer;
    }

    private static async Task<MaterialCatalog> CreateMaterialAsync(AppDbContext context, string name = "Cemento")
    {
        var material = new MaterialCatalog { Name = name, CreatedAt = DateTime.UtcNow };
        context.MaterialCatalogs.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private static async Task<Worker> CreateWorkerAsync(AppDbContext context, string name = "Trabajador Test")
    {
        var worker = new Worker
        {
            Name = name,
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = 5m,
            CreatedAt = DateTime.UtcNow
        };
        context.Workers.Add(worker);
        await context.SaveChangesAsync();
        return worker;
    }

    private static async Task<ProjectInventoryItem> CreateInventoryItemAsync(AppDbContext context, int projectId, int materialId, decimal currentQuantity, decimal referenceUnitCost)
    {
        var item = new ProjectInventoryItem
        {
            ProjectId = projectId,
            MaterialId = materialId,
            CurrentQuantity = currentQuantity,
            ReferenceUnitCost = referenceUnitCost,
            CreatedAt = DateTime.UtcNow
        };
        context.ProjectInventoryItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private static async Task<(ProjectDto Project, int ManagerUserId, int ProjectAdminUserId)> CreateActiveProjectAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var projectAdmin = await TestUserFactory.CreateAsync(context, $"pa-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateApprovedBudgetAsync(context, customer.Id, branch.Id, manager.Id);
        var offer = await CreateAcceptedOfferAsync(context, budget.Id, customer.Id, manager.Id);

        var projectService = ServiceFactory.CreateProjectService(context);
        var project = await projectService.CreateProjectAsync(new CreateProjectDto
        {
            OfferId = offer.Id,
            BranchId = branch.Id,
            StartDate = new DateTime(2026, 3, 1)
        }, manager.Id);

        return (project, manager.Id, projectAdmin.Id);
    }

    private static CreateSiteLogDto BuildCreateDto(
        int projectId,
        DateTime weekStart,
        List<SiteLogWorkerDto>? workers = null,
        List<SiteLogMaterialDto>? materials = null,
        List<SiteLogEquipmentDto>? equipment = null) => new()
    {
        ProjectId = projectId,
        WeekStart = weekStart,
        WeekEnd = weekStart.AddDays(6),
        TaskDescription = "Vaciado de fundaciones",
        PendingTasks = "Armado de columnas",
        Workers = workers ?? new List<SiteLogWorkerDto>(),
        Materials = materials ?? new List<SiteLogMaterialDto>(),
        Equipment = equipment ?? new List<SiteLogEquipmentDto>()
    };

    [Fact]
    public async Task CreateAsync_WithThreeChildLists_PersistsAll()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var worker = await CreateWorkerAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var result = await service.CreateAsync(BuildCreateDto(
            project.Id,
            new DateTime(2026, 3, 2),
            workers: new List<SiteLogWorkerDto> { new() { WorkerId = worker.Id, HoursWorked = 40 } },
            materials: new List<SiteLogMaterialDto> { new() { MaterialId = material.Id, QuantityUsed = 5 } },
            equipment: new List<SiteLogEquipmentDto> { new() { EquipmentType = EquipmentType.Heavy, Description = "Excavadora" } }), projectAdminId);

        result.Status.Should().Be(SiteLogStatus.Draft);
        result.Workers.Should().ContainSingle(w => w.WorkerId == worker.Id && w.HoursWorked == 40);
        result.Materials.Should().ContainSingle(m => m.MaterialId == material.Id && m.QuantityUsed == 5);
        result.Equipment.Should().ContainSingle(e => e.EquipmentType == EquipmentType.Heavy && e.Description == "Excavadora");
    }

    [Fact]
    public async Task CreateAsync_DuplicateProjectAndWeek_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);
        var weekStart = new DateTime(2026, 3, 2);

        await service.CreateAsync(BuildCreateDto(project.Id, weekStart), projectAdminId);

        var act = async () => await service.CreateAsync(BuildCreateDto(project.Id, weekStart), projectAdminId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_InDraft_SyncsAggregateCompletely()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var worker1 = await CreateWorkerAsync(context, "Trabajador 1");
        var worker2 = await CreateWorkerAsync(context, "Trabajador 2");
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(
            project.Id,
            new DateTime(2026, 3, 2),
            workers: new List<SiteLogWorkerDto> { new() { WorkerId = worker1.Id, HoursWorked = 10 } },
            materials: new List<SiteLogMaterialDto> { new() { MaterialId = material.Id, QuantityUsed = 3 } }), projectAdminId);

        var updated = await service.UpdateAsync(created.Id, new UpdateSiteLogDto
        {
            TaskDescription = "Actualizado",
            Workers = new List<SiteLogWorkerDto>
            {
                new() { WorkerId = worker1.Id, HoursWorked = 20 },
                new() { WorkerId = worker2.Id, HoursWorked = 15 }
            },
            Materials = new List<SiteLogMaterialDto>()
        });

        updated.TaskDescription.Should().Be("Actualizado");
        updated.Workers.Should().HaveCount(2);
        updated.Workers.Should().ContainSingle(w => w.WorkerId == worker1.Id && w.HoursWorked == 20);
        updated.Workers.Should().ContainSingle(w => w.WorkerId == worker2.Id && w.HoursWorked == 15);
        updated.Materials.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_InReview_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var updated = await service.UpdateAsync(created.Id, new UpdateSiteLogDto { TaskDescription = "En revisión, editado" });

        updated.Status.Should().Be(SiteLogStatus.Review);
        updated.TaskDescription.Should().Be("En revisión, editado");
    }

    [Fact]
    public async Task UpdateAsync_Approved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);
        await service.SubmitToReviewAsync(created.Id);
        await service.ApproveAsync(created.Id, managerId);

        var act = async () => await service.UpdateAsync(created.Id, new UpdateSiteLogDto { TaskDescription = "No debería aplicar" });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task SubmitToReviewAsync_FromDraft_MovesToReview()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);

        var result = await service.SubmitToReviewAsync(created.Id);

        result.Status.Should().Be(SiteLogStatus.Review);
    }

    [Fact]
    public async Task RevertToDraftAsync_FromReview_MovesToDraft()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var result = await service.RevertToDraftAsync(created.Id, new RevertToDraftDto { Reason = "Faltan datos" });

        result.Status.Should().Be(SiteLogStatus.Draft);
    }

    [Fact]
    public async Task ApproveAsync_DeductsInventory_CalculatesTotalMaterials_AddsWorkedHours_DoesNotTouchDirectExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var worker = await CreateWorkerAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 20, referenceUnitCost: 15);

        var storedProjectBefore = await context.Projects.FindAsync(project.Id);
        var directExpensesBefore = storedProjectBefore!.CurrentDirectExpenses;

        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(
            project.Id,
            new DateTime(2026, 3, 2),
            workers: new List<SiteLogWorkerDto> { new() { WorkerId = worker.Id, HoursWorked = 12 } },
            materials: new List<SiteLogMaterialDto> { new() { MaterialId = material.Id, QuantityUsed = 8 } }), projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.Status.Should().Be(SiteLogStatus.Approved);
        approved.ApprovedByUserId.Should().Be(managerId);
        approved.TotalMaterials.Should().Be(120m); // 8 * 15

        var inventoryItem = await context.ProjectInventoryItems
            .FirstAsync(i => i.ProjectId == project.Id && i.MaterialId == material.Id);
        inventoryItem.CurrentQuantity.Should().Be(12m); // 20 - 8

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.TotalWorkedHours.Should().Be(12m);
        storedProject.CurrentDirectExpenses.Should().Be(directExpensesBefore);
    }

    [Fact]
    public async Task ApproveAsync_InsufficientInventory_ThrowsAndAppliesNothingPartially()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material1 = await CreateMaterialAsync(context, "Cemento");
        var material2 = await CreateMaterialAsync(context, "Arena");
        await CreateInventoryItemAsync(context, project.Id, material1.Id, currentQuantity: 20, referenceUnitCost: 10);
        await CreateInventoryItemAsync(context, project.Id, material2.Id, currentQuantity: 2, referenceUnitCost: 5);

        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(
            project.Id,
            new DateTime(2026, 3, 2),
            materials: new List<SiteLogMaterialDto>
            {
                new() { MaterialId = material1.Id, QuantityUsed = 10 },
                new() { MaterialId = material2.Id, QuantityUsed = 5 } // more than the 2 available
            }), projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var act = async () => await service.ApproveAsync(created.Id, managerId);

        await act.Should().ThrowAsync<ValidationAppException>();

        var inventoryItem1 = await context.ProjectInventoryItems.FirstAsync(i => i.ProjectId == project.Id && i.MaterialId == material1.Id);
        inventoryItem1.CurrentQuantity.Should().Be(20m); // untouched, no partial application

        var inventoryItem2 = await context.ProjectInventoryItems.FirstAsync(i => i.ProjectId == project.Id && i.MaterialId == material2.Id);
        inventoryItem2.CurrentQuantity.Should().Be(2m);

        var storedSiteLog = await context.SiteLogs.FindAsync(created.Id);
        storedSiteLog!.Status.Should().Be(SiteLogStatus.Review);
    }

    [Fact]
    public async Task ApproveAsync_NotReview_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);

        var act = async () => await service.ApproveAsync(created.Id, managerId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_Draft_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);

        await service.DeleteAsync(created.Id);

        var stored = await context.SiteLogs.FindAsync(created.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Review_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_Approved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(BuildCreateDto(project.Id, new DateTime(2026, 3, 2)), projectAdminId);
        await service.SubmitToReviewAsync(created.Id);
        await service.ApproveAsync(created.Id, managerId);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ApproveAsync_ZeroElapsedWeeks_SetsProgressPercentageToZero()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate,
            TaskDescription = "Semana inicial"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.ProgressPercentage.Should().Be(0m);
    }

    [Fact]
    public async Task ApproveAsync_AtMidpointOfEstimatedDuration_SetsProgressPercentageTo50()
    {
        using var context = TestDbContextFactory.Create();
        // CreateAcceptedOfferAsync sets EstimatedDurationWeeks = 10, so 5 elapsed weeks = 50%.
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate.AddDays(35),
            TaskDescription = "Semana intermedia"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.ProgressPercentage.Should().Be(50m);
    }

    [Fact]
    public async Task ApproveAsync_BeyondEstimatedDuration_CapsProgressPercentageAt100()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate.AddDays(140), // 20 weeks, double the 10-week estimate
            TaskDescription = "Semana muy avanzada"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.ProgressPercentage.Should().Be(100m);
    }

    [Fact]
    public async Task ApproveAsync_CreatesProjectProgressRecord()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate.AddDays(35),
            TaskDescription = "Semana intermedia"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        await service.ApproveAsync(created.Id, managerId);

        var progressRecords = await context.ProjectProgresses.Where(p => p.SiteLogId == created.Id).ToListAsync();
        progressRecords.Should().ContainSingle();
        progressRecords[0].ProjectId.Should().Be(project.Id);
        progressRecords[0].ProgressPercentage.Should().Be(50m);
    }
}
