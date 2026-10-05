using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.Warehouse;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Commercial;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Offers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LvTest.Services.Warehouse;

public class InventoryMovementServiceTests
{
    private static async Task<Branch> CreateBranchAsync(
        AppDbContext context,
        int operationsDirectorId,
        BranchType branchType,
        string name = "Sucursal Test"
    )
    {
        var branch = new Branch
        {
            Name = name,
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = branchType,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

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

    private static async Task<ProjectDto> CreateActiveProjectAsync(AppDbContext context)
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
        var customer = await CreateProjectCustomerAsync(context);
        var projectBranch = await CreateBranchAsync(
            context,
            director.Id,
            BranchType.Office,
            "Oficina Proyecto"
        );
        var budget = await CreateApprovedBudgetAsync(
            context,
            customer.Id,
            projectBranch.Id,
            manager.Id
        );
        var offer = await CreateAcceptedOfferAsync(context, budget.Id, customer.Id, manager.Id);

        var projectService = ServiceFactory.CreateProjectService(context);
        return await projectService.CreateProjectAsync(
            new CreateProjectDto
            {
                OfferId = offer.Id,
                BranchId = projectBranch.Id,
                StartDate = new DateTime(2026, 3, 1),
            },
            manager.Id
        );
    }

    private static async Task<Product> CreateValidatedProductAsync(
        AppDbContext context,
        int createdByUserId,
        string sku,
        decimal unitCost = 700m
    )
    {
        var product = new Product
        {
            Name = "Producto " + sku,
            Sku = sku,
            UnitPrice = 1000m,
            UnitCost = unitCost,
            Status = ProductStatus.Validated,
            ActiveStatus = true,
            CreatedByUserId = createdByUserId,
            ValidatedByUserId = createdByUserId,
            ValidatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static async Task<BranchInventory> CreateInventoryAsync(
        AppDbContext context,
        int branchId,
        int productId,
        decimal quantity
    )
    {
        var inventory = new BranchInventory
        {
            BranchId = branchId,
            ProductId = productId,
            Quantity = quantity,
            MinimumStock = 0,
            CreatedAt = DateTime.UtcNow,
        };
        context.BranchInventories.Add(inventory);
        await context.SaveChangesAsync();
        return inventory;
    }

    private static CreateInventoryMovementDto BuildDtoToBranch(
        int originBranchId,
        int destinationBranchId,
        int productId,
        decimal quantity = 5
    ) =>
        new()
        {
            OriginBranchId = originBranchId,
            DestinationBranchId = destinationBranchId,
            ProductId = productId,
            Quantity = quantity,
        };

    private static CreateInventoryMovementDto BuildDtoToProject(
        int originBranchId,
        int destinationProjectId,
        int productId,
        decimal quantity = 5
    ) =>
        new()
        {
            OriginBranchId = originBranchId,
            DestinationProjectId = destinationProjectId,
            ProductId = productId,
            Quantity = quantity,
        };

    [Fact]
    public async Task CreateAsync_ValidDestinationBranch_CreatesSentMovement()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im1@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-1");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var result = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            user.Id
        );

        result.Status.Should().Be(InventoryMovementStatus.Sent);
        result.ValidatedByUserId.Should().BeNull();
        var origin = await context.BranchInventories.FirstAsync(i =>
            i.BranchId == warehouse.Id && i.ProductId == product.Id
        );
        origin.Quantity.Should().Be(20m, "creating a movement must not touch inventory yet");
    }

    [Fact]
    public async Task CreateAsync_ValidDestinationProject_CreatesSentMovement()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im2@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-2");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var project = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var result = await service.CreateAsync(
            BuildDtoToProject(warehouse.Id, project.Id, product.Id, 5),
            user.Id
        );

        result.Status.Should().Be(InventoryMovementStatus.Sent);
    }

    [Fact]
    public async Task CreateAsync_OriginNotWarehouse_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im3@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var commerceOrigin = await CreateBranchAsync(
            context,
            user.Id,
            BranchType.Commercial,
            "Comercio Origen"
        );
        var commerceDestination = await CreateBranchAsync(
            context,
            user.Id,
            BranchType.Commercial,
            "Comercio Destino"
        );
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-3");
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () =>
            service.CreateAsync(
                BuildDtoToBranch(commerceOrigin.Id, commerceDestination.Id, product.Id),
                user.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_DestinationBranchNotCommercial_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im4@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouseOrigin = await CreateBranchAsync(
            context,
            user.Id,
            BranchType.Warehouse,
            "Bodega Origen"
        );
        var warehouseDestination = await CreateBranchAsync(
            context,
            user.Id,
            BranchType.Warehouse,
            "Bodega Destino"
        );
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-4");
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () =>
            service.CreateAsync(
                BuildDtoToBranch(warehouseOrigin.Id, warehouseDestination.Id, product.Id),
                user.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_NoDestination_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im5@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-5");
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () =>
            service.CreateAsync(
                new CreateInventoryMovementDto
                {
                    OriginBranchId = warehouse.Id,
                    ProductId = product.Id,
                    Quantity = 5,
                },
                user.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_BothDestinations_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im6@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-6");
        var project = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () =>
            service.CreateAsync(
                new CreateInventoryMovementDto
                {
                    OriginBranchId = warehouse.Id,
                    DestinationBranchId = commerce.Id,
                    DestinationProjectId = project.Id,
                    ProductId = product.Id,
                    Quantity = 5,
                },
                user.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ValidateAsync_ApproveToBranch_DecrementsOriginIncrementsDestination()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im10@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-10");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            user.Id
        );

        var validated = await service.ValidateAsync(
            sent.Id,
            true,
            user.Id,
            TestRoles.GeneralManager
        );

        validated.Status.Should().Be(InventoryMovementStatus.Accepted);
        var origin = await context.BranchInventories.FirstAsync(i =>
            i.BranchId == warehouse.Id && i.ProductId == product.Id
        );
        origin.Quantity.Should().Be(15m);
        var destination = await context.BranchInventories.FirstAsync(i =>
            i.BranchId == commerce.Id && i.ProductId == product.Id
        );
        destination.Quantity.Should().Be(5m);
    }

    [Fact]
    public async Task ValidateAsync_ApproveToProject_IncrementsProjectInventoryWithoutTouchingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im11@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var product = await CreateValidatedProductAsync(
            context,
            user.Id,
            "SKU-IM-11",
            unitCost: 650m
        );
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var project = await CreateActiveProjectAsync(context);
        var expensesBefore = (project.CurrentDirectExpenses, project.PendingExpenses);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToProject(warehouse.Id, project.Id, product.Id, 5),
            user.Id
        );

        var validated = await service.ValidateAsync(
            sent.Id,
            true,
            user.Id,
            TestRoles.GeneralManager
        );

        validated.Status.Should().Be(InventoryMovementStatus.Accepted);
        var inventoryItem = await context.ProjectInventoryItems.FirstAsync(i =>
            i.ProjectId == project.Id && i.ProductId == product.Id
        );
        inventoryItem.CurrentQuantity.Should().Be(5m);
        inventoryItem.ReferenceUnitCost.Should().Be(650m);
        inventoryItem.MaterialId.Should().BeNull();
        var projectAfter = await context.Projects.FindAsync(project.Id);
        projectAfter!
            .CurrentDirectExpenses.Should()
            .Be(
                expensesBefore.CurrentDirectExpenses,
                "the material's cost was already recognized when it entered the warehouse via ProductIncorporationTicket"
            );
        projectAfter.PendingExpenses.Should().Be(expensesBefore.PendingExpenses);
    }

    [Fact]
    public async Task ValidateAsync_Deny_NoInventoryChanges()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im12@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-12");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            user.Id
        );

        var denied = await service.ValidateAsync(sent.Id, false, user.Id, TestRoles.GeneralManager);

        denied.Status.Should().Be(InventoryMovementStatus.Denied);
        var origin = await context.BranchInventories.FirstAsync(i =>
            i.BranchId == warehouse.Id && i.ProductId == product.Id
        );
        origin.Quantity.Should().Be(20m);
        (
            await context.BranchInventories.AnyAsync(i =>
                i.BranchId == commerce.Id && i.ProductId == product.Id
            )
        )
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ApproveInsufficientOriginStock_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im13@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-13");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 3m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            user.Id
        );

        var act = () => service.ValidateAsync(sent.Id, true, user.Id, TestRoles.GeneralManager);

        await act.Should().ThrowAsync<ValidationAppException>();
        var origin = await context.BranchInventories.FirstAsync(i =>
            i.BranchId == warehouse.Id && i.ProductId == product.Id
        );
        origin.Quantity.Should().Be(3m, "a failed validation must not decrement stock");
    }

    [Fact]
    public async Task ValidateAsync_ByBranchAdmin_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "im14dir@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var branchAdmin = await TestUserFactory.CreateAsync(
            context,
            "im14ba@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var warehouse = await CreateBranchAsync(
            context,
            director.Id,
            BranchType.Warehouse,
            "Bodega"
        );
        var commerce = await CreateBranchAsync(
            context,
            director.Id,
            BranchType.Commercial,
            "Comercio"
        );
        var product = await CreateValidatedProductAsync(context, director.Id, "SKU-IM-14");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            director.Id
        );

        var act = () => service.ValidateAsync(sent.Id, true, branchAdmin.Id, TestRoles.BranchAdmin);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteAsync_SentMovement_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im20@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-20");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            user.Id
        );

        await service.DeleteAsync(sent.Id, TestRoles.GeneralManager);

        (await context.InventoryMovements.FindAsync(sent.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_AcceptedMovement_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im21@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-21");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            user.Id
        );
        await service.ValidateAsync(sent.Id, true, user.Id, TestRoles.GeneralManager);

        var act = () => service.DeleteAsync(sent.Id, TestRoles.GeneralManager);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_ByBranchAdmin_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "im22dir@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var branchAdmin = await TestUserFactory.CreateAsync(
            context,
            "im22ba@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var warehouse = await CreateBranchAsync(
            context,
            director.Id,
            BranchType.Warehouse,
            "Bodega"
        );
        var commerce = await CreateBranchAsync(
            context,
            director.Id,
            BranchType.Commercial,
            "Comercio"
        );
        var product = await CreateValidatedProductAsync(context, director.Id, "SKU-IM-22");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            director.Id
        );

        var act = () => service.DeleteAsync(sent.Id, TestRoles.BranchAdmin);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMovement()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "im23@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-23");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(
            BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5),
            user.Id
        );

        var result = await service.GetByIdAsync(sent.Id);

        result.Id.Should().Be(sent.Id);
    }
}
