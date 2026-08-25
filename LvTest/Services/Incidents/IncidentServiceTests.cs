using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Incidents;
using LvApplication.DTOs.Projects;
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

namespace LvTest.Services.Incidents;

public class IncidentServiceTests
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

    private static async Task<Budget> CreateApprovedBudgetAsync(
        AppDbContext context,
        int customerId,
        int branchId,
        int createdByUserId
    )
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
            CreatedAt = DateTime.UtcNow,
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    private static async Task<Offer> CreateAcceptedOfferAsync(
        AppDbContext context,
        int budgetId,
        int customerId,
        int createdByUserId
    )
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
            CreatedAt = DateTime.UtcNow,
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();
        return offer;
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

    private static async Task<Worker> CreateWorkerAsync(
        AppDbContext context,
        string name = "Trabajador Test",
        decimal hourlyRate = 5m
    )
    {
        var worker = new Worker
        {
            Name = name,
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = hourlyRate,
            CreatedAt = DateTime.UtcNow,
        };
        context.Workers.Add(worker);
        await context.SaveChangesAsync();
        return worker;
    }

    private static async Task<ProjectInventoryItem> CreateInventoryItemAsync(
        AppDbContext context,
        int projectId,
        int materialId,
        decimal currentQuantity,
        decimal referenceUnitCost
    )
    {
        var item = new ProjectInventoryItem
        {
            ProjectId = projectId,
            MaterialId = materialId,
            CurrentQuantity = currentQuantity,
            ReferenceUnitCost = referenceUnitCost,
            CreatedAt = DateTime.UtcNow,
        };
        context.ProjectInventoryItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private static async Task<(
        ProjectDto Project,
        int ManagerUserId,
        int ProjectAdminUserId
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
        var budget = await CreateApprovedBudgetAsync(context, customer.Id, branch.Id, manager.Id);
        var offer = await CreateAcceptedOfferAsync(context, budget.Id, customer.Id, manager.Id);

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
    public async Task CreateAsync_CalculatesTotalCost_AndAddsToPendingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(
            context,
            project.Id,
            material.Id,
            currentQuantity: 50,
            referenceUnitCost: 10
        );
        var worker = await CreateWorkerAsync(context, hourlyRate: 8);
        var service = ServiceFactory.CreateIncidentService(context);

        var storedProjectBefore = await context.Projects.FindAsync(project.Id);
        var pendingBefore = storedProjectBefore!.PendingExpenses;

        var result = await service.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 3, 10),
                Description = "Reparación de tubería",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 4 },
                }, // 4 * 10 = 40
                Workers = new List<IncidentWorkerDto>
                {
                    new() { WorkerId = worker.Id, HoursUsed = 6 },
                }, // 6 * 8 = 48
            },
            projectAdminId
        );

        result.Status.Should().Be(IncidentStatus.Draft);
        result.TotalCost.Should().Be(88m);

        var storedProjectAfter = await context.Projects.FindAsync(project.Id);
        storedProjectAfter!.PendingExpenses.Should().Be(pendingBefore + 88m);
    }

    [Fact]
    public async Task UpdateAsync_InDraft_AdjustsPendingExpensesByDifference()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(
            context,
            project.Id,
            material.Id,
            currentQuantity: 50,
            referenceUnitCost: 10
        );
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 3, 10),
                Description = "Reparación de tubería",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 4 },
                }, // 40
                Workers = new List<IncidentWorkerDto>(),
            },
            projectAdminId
        );

        var pendingAfterCreate = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateIncidentDto
            {
                Date = new DateTime(2026, 3, 11),
                Description = "Reparación de tubería (actualizado)",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 9 },
                }, // 90
                Workers = new List<IncidentWorkerDto>(),
            }
        );

        updated.TotalCost.Should().Be(90m);

        var pendingAfterUpdate = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;
        pendingAfterUpdate.Should().Be(pendingAfterCreate + (90m - 40m));
    }

    [Fact]
    public async Task UpdateAsync_Approved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(
            context,
            project.Id,
            material.Id,
            currentQuantity: 50,
            referenceUnitCost: 10
        );
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 3, 10),
                Description = "Reparación de tubería",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 4 },
                },
                Workers = new List<IncidentWorkerDto>(),
            },
            projectAdminId
        );
        await service.ApproveAsync(created.Id, managerId);

        var act = async () =>
            await service.UpdateAsync(
                created.Id,
                new UpdateIncidentDto
                {
                    Date = new DateTime(2026, 3, 11),
                    Description = "No debería aplicar",
                    Materials = new List<IncidentMaterialDto>(),
                    Workers = new List<IncidentWorkerDto>(),
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ApproveAsync_MovesPendingToDirectExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(
            context,
            project.Id,
            material.Id,
            currentQuantity: 50,
            referenceUnitCost: 10
        );
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 3, 10),
                Description = "Reparación de tubería",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 4 },
                }, // 40
                Workers = new List<IncidentWorkerDto>(),
            },
            projectAdminId
        );

        var pendingBeforeApprove = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;
        var directBeforeApprove = (
            await context.Projects.FindAsync(project.Id)
        )!.CurrentDirectExpenses;

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.Status.Should().Be(IncidentStatus.Approved);
        approved.ApprovedByUserId.Should().Be(managerId);

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(pendingBeforeApprove - 40m);
        storedProject.CurrentDirectExpenses.Should().Be(directBeforeApprove + 40m);

        var inventoryItem = await context.ProjectInventoryItems.FirstAsync(i =>
            i.ProjectId == project.Id && i.MaterialId == material.Id
        );
        inventoryItem.CurrentQuantity.Should().Be(50m); // untouched — approving an Incident does not deduct inventory
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(
            context,
            project.Id,
            material.Id,
            currentQuantity: 50,
            referenceUnitCost: 10
        );
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 3, 10),
                Description = "Reparación de tubería",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 4 },
                },
                Workers = new List<IncidentWorkerDto>(),
            },
            projectAdminId
        );
        await service.ApproveAsync(created.Id, managerId);

        var act = async () => await service.ApproveAsync(created.Id, managerId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_Draft_Succeeds_SubtractsPendingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(
            context,
            project.Id,
            material.Id,
            currentQuantity: 50,
            referenceUnitCost: 10
        );
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 3, 10),
                Description = "Reparación de tubería",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 4 },
                }, // 40
                Workers = new List<IncidentWorkerDto>(),
            },
            projectAdminId
        );

        var pendingBeforeDelete = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;

        await service.DeleteAsync(created.Id);

        var stored = await context.Incidents.FindAsync(created.Id);
        stored.Should().BeNull();

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(pendingBeforeDelete - 40m);
    }

    [Fact]
    public async Task DeleteAsync_Approved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(
            context,
            project.Id,
            material.Id,
            currentQuantity: 50,
            referenceUnitCost: 10
        );
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 3, 10),
                Description = "Reparación de tubería",
                Materials = new List<IncidentMaterialDto>
                {
                    new() { MaterialId = material.Id, Quantity = 4 },
                },
                Workers = new List<IncidentWorkerDto>(),
            },
            projectAdminId
        );
        await service.ApproveAsync(created.Id, managerId);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
