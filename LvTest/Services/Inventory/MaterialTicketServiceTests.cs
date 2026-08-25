using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Inventory;
using LvApplication.DTOs.Projects;
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

namespace LvTest.Services.Inventory;

public class MaterialTicketServiceTests
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

    private static async Task<Supplier> CreateSupplierAsync(AppDbContext context)
    {
        var supplier = new Supplier
        {
            Name = "Test Supplier",
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

    private static CreateMaterialTicketDto BuildCreateTicketDto(
        int supplierId,
        int materialId,
        decimal quantity = 10,
        decimal unitPrice = 20,
        decimal? discount = null
    ) =>
        new()
        {
            SupplierId = supplierId,
            MaterialId = materialId,
            Description = "Compra de prueba",
            Quantity = quantity,
            UnitPrice = unitPrice,
            Discount = discount,
        };

    [Fact]
    public async Task CreateAsync_ValidTicket_AddsTotalToPendingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var result = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(
                supplier.Id,
                material.Id,
                quantity: 10,
                unitPrice: 20,
                discount: 15
            ),
            projectAdminId
        );

        result.Status.Should().Be(MaterialTicketStatus.Review);
        result.MaterialName.Should().Be("Cemento");
        result.Subtotal.Should().Be(200m);
        result.Total.Should().Be(185m);

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(185m);
    }

    [Fact]
    public async Task UpdateAsync_InReview_AdjustsPendingExpensesByDifference()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var otherMaterial = await CreateMaterialAsync(context, "Arena");
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id, quantity: 10, unitPrice: 20),
            projectAdminId
        );
        // Initial total = 200, so PendingExpenses = 200.

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateMaterialTicketDto
            {
                SupplierId = supplier.Id,
                MaterialId = otherMaterial.Id,
                Quantity = 10,
                UnitPrice = 30,
            }
        );
        // New total = 300, difference = +100.

        updated.Total.Should().Be(300m);
        updated.MaterialName.Should().Be("Arena");

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(300m);
    }

    [Fact]
    public async Task UpdateAsync_NotReview_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id),
            projectAdminId
        );
        await service.ApplyAsync(created.Id);

        var act = async () =>
            await service.UpdateAsync(
                created.Id,
                new UpdateMaterialTicketDto
                {
                    SupplierId = supplier.Id,
                    MaterialId = material.Id,
                    Quantity = 5,
                    UnitPrice = 10,
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ApplyAsync_MovesAmountFromPendingToDirectExpensesAndCreatesInventoryItem()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id, quantity: 10, unitPrice: 20),
            projectAdminId
        );

        var result = await service.ApplyAsync(created.Id);

        result.Status.Should().Be(MaterialTicketStatus.Applied);

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(0m);
        storedProject.CurrentDirectExpenses.Should().Be(200m);

        var inventoryItem = await context.ProjectInventoryItems.FirstOrDefaultAsync(i =>
            i.ProjectId == project.Id && i.MaterialId == material.Id
        );
        inventoryItem.Should().NotBeNull();
        inventoryItem!.CurrentQuantity.Should().Be(10m);
        inventoryItem.ReferenceUnitCost.Should().Be(20m);
    }

    [Fact]
    public async Task ApplyAsync_ExistingInventoryItem_IncrementsQuantityInsteadOfDuplicating()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var firstTicket = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id, quantity: 10, unitPrice: 20),
            projectAdminId
        );
        await service.ApplyAsync(firstTicket.Id);

        var secondTicket = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id, quantity: 5, unitPrice: 25),
            projectAdminId
        );
        await service.ApplyAsync(secondTicket.Id);

        var inventoryItems = await context
            .ProjectInventoryItems.Where(i =>
                i.ProjectId == project.Id && i.MaterialId == material.Id
            )
            .ToListAsync();

        inventoryItems.Should().HaveCount(1);
        inventoryItems[0].CurrentQuantity.Should().Be(15m);
        inventoryItems[0].ReferenceUnitCost.Should().Be(25m);
    }

    [Fact]
    public async Task ApplyAsync_NotReview_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id),
            projectAdminId
        );
        await service.ApplyAsync(created.Id);

        var act = async () => await service.ApplyAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ArchiveAsync_SubtractsFromPendingExpensesWithoutTouchingInventory()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id, quantity: 10, unitPrice: 20),
            projectAdminId
        );

        var result = await service.ArchiveAsync(created.Id);

        result.Status.Should().Be(MaterialTicketStatus.Archived);

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(0m);
        storedProject.CurrentDirectExpenses.Should().Be(0m);

        var inventoryItem = await context.ProjectInventoryItems.FirstOrDefaultAsync(i =>
            i.ProjectId == project.Id && i.MaterialId == material.Id
        );
        inventoryItem.Should().BeNull();
    }

    [Fact]
    public async Task ArchiveAsync_NotReview_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id),
            projectAdminId
        );
        await service.ArchiveAsync(created.Id);

        var act = async () => await service.ArchiveAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_InReview_DeletesAndSubtractsPendingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id, quantity: 10, unitPrice: 20),
            projectAdminId
        );

        await service.DeleteAsync(created.Id);

        var storedTicket = await context.MaterialTickets.FindAsync(created.Id);
        storedTicket.Should().BeNull();

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(0m);
    }

    [Fact]
    public async Task DeleteAsync_Applied_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id),
            projectAdminId
        );
        await service.ApplyAsync(created.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_Archived_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id),
            projectAdminId
        );
        await service.ArchiveAsync(created.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ApplyAsync_RecalculatesMaterialsUsedCount()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var cement = await CreateMaterialAsync(context, "Cemento");
        var sand = await CreateMaterialAsync(context, "Arena");
        var service = ServiceFactory.CreateProjectService(context);
        var ticketService = ServiceFactory.CreateMaterialTicketService(context);

        var initialProject = await service.GetByIdAsync(project.Id);
        initialProject.MaterialsUsedCount.Should().Be(0);

        var cementTicket = await ticketService.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, cement.Id, quantity: 10, unitPrice: 20),
            projectAdminId
        );
        await ticketService.ApplyAsync(cementTicket.Id);

        var afterFirstApply = await service.GetByIdAsync(project.Id);
        afterFirstApply.MaterialsUsedCount.Should().Be(1);

        var sandTicket = await ticketService.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, sand.Id, quantity: 5, unitPrice: 8),
            projectAdminId
        );
        await ticketService.ApplyAsync(sandTicket.Id);

        var afterSecondApply = await service.GetByIdAsync(project.Id);
        afterSecondApply.MaterialsUsedCount.Should().Be(2);
    }

    [Fact]
    public async Task GetInventoryAsync_ReturnsAppliedItemsForProject()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var created = await service.CreateAsync(
            project.Id,
            BuildCreateTicketDto(supplier.Id, material.Id, quantity: 10, unitPrice: 20),
            projectAdminId
        );
        await service.ApplyAsync(created.Id);

        var inventory = await service.GetInventoryAsync(project.Id);

        inventory
            .Should()
            .ContainSingle(i =>
                i.MaterialId == material.Id
                && i.CurrentQuantity == 10m
                && i.ReferenceUnitCost == 20m
            );
    }

    [Fact]
    public async Task GetAllByProjectAsync_ReturnsOnlyTicketsForThatProject()
    {
        using var context = TestDbContextFactory.Create();
        var (projectA, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var (projectB, _, _) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);
        var service = ServiceFactory.CreateMaterialTicketService(context);

        var ticketA = await service.CreateAsync(
            projectA.Id,
            BuildCreateTicketDto(supplier.Id, material.Id),
            projectAdminId
        );
        await service.CreateAsync(
            projectB.Id,
            BuildCreateTicketDto(supplier.Id, material.Id),
            projectAdminId
        );

        var result = await service.GetAllByProjectAsync(projectA.Id, pageNumber: 1, pageSize: 20);

        result.TotalCount.Should().Be(1);
        result.Items.Should().OnlyContain(t => t.Id == ticketA.Id);
    }
}
