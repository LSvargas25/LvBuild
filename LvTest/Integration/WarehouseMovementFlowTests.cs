using FluentAssertions;
using LvApplication.DTOs.Warehouse;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LvTest.Integration;

/// <summary>
/// Exercises the Bodega -> Sucursal movement flow against real PostgreSQL, directly
/// covering the inventory_movements table + ck_inventory_movements_exactly_one_destination
/// check constraint (added with the warehouse module in Tarea 1), which until
/// now had never been applied against a real engine.
/// </summary>
[Collection(PostgresIntegrationCollection.Name)]
public class WarehouseMovementFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _db;

    public WarehouseMovementFlowTests(PostgresFixture db)
    {
        _db = db;
    }

    public Task InitializeAsync() => _db.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ValidateAsync_ApprovedMovementBetweenBranches_MovesStockAgainstRealPostgres()
    {
        await using var context = _db.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = await TestUserFactory.CreateAsync(
            context,
            $"gm-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );

        var warehouse = new Branch
        {
            Name = "Bodega Integracion",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        var commerce = new Branch
        {
            Name = "Comercio Integracion",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Commercial,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.AddRange(warehouse, commerce);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Producto Integracion",
            Sku = $"SKU-INT-{Guid.NewGuid():N}"[..20],
            UnitPrice = 1000m,
            UnitCost = 700m,
            Status = ProductStatus.Validated,
            ActiveStatus = true,
            CreatedByUserId = user.Id,
            ValidatedByUserId = user.Id,
            ValidatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.BranchInventories.Add(
            new BranchInventory
            {
                BranchId = warehouse.Id,
                ProductId = product.Id,
                Quantity = 20m,
                MinimumStock = 0,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var service = ServiceFactory.CreateInventoryMovementService(context);

        var sent = await service.CreateAsync(
            new CreateInventoryMovementDto
            {
                OriginBranchId = warehouse.Id,
                DestinationBranchId = commerce.Id,
                ProductId = product.Id,
                Quantity = 5m,
            },
            user.Id
        );

        sent.Status.Should().Be(InventoryMovementStatus.Sent);

        var validated = await service.ValidateAsync(
            sent.Id,
            true,
            user.Id,
            new[] { "GeneralManager" }
        );

        validated.Status.Should().Be(InventoryMovementStatus.Accepted);

        context.ChangeTracker.Clear();
        var origin = await context
            .BranchInventories.AsNoTracking()
            .FirstAsync(i => i.BranchId == warehouse.Id && i.ProductId == product.Id);
        var destination = await context
            .BranchInventories.AsNoTracking()
            .FirstAsync(i => i.BranchId == commerce.Id && i.ProductId == product.Id);

        origin.Quantity.Should().Be(15m);
        destination.Quantity.Should().Be(5m);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task CreateAsync_MovementWithNeitherOrBothDestinations_IsRejectedByRealCheckConstraint()
    {
        await using var context = _db.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = await TestUserFactory.CreateAsync(
            context,
            $"gm-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );

        var warehouse = new Branch
        {
            Name = "Bodega Integracion CK",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(warehouse);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Producto Integracion CK",
            Sku = $"SKU-INT-{Guid.NewGuid():N}"[..20],
            UnitPrice = 1000m,
            UnitCost = 700m,
            Status = ProductStatus.Validated,
            ActiveStatus = true,
            CreatedByUserId = user.Id,
            ValidatedByUserId = user.Id,
            ValidatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Bypasses the service-layer validation on purpose: writes directly to prove
        // the database-level ck_inventory_movements_exactly_one_destination itself
        // rejects an invalid row, as a second line of defense beyond application code.
        context.InventoryMovements.Add(
            new LvDomain.Entities.Warehouse.InventoryMovement
            {
                OriginBranchId = warehouse.Id,
                DestinationBranchId = null,
                DestinationProjectId = null,
                ProductId = product.Id,
                Quantity = 1m,
                Status = InventoryMovementStatus.Sent,
                SentByUserId = user.Id,
                SentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            }
        );

        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();

        await transaction.RollbackAsync();
    }
}
