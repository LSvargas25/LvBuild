using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Commercial;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Entities.Suppliers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LvTest.Services.Commercial;

public class ProductIncorporationTicketServiceTests
{
    private static async Task<Branch> CreateBranchAsync(
        AppDbContext context,
        int operationsDirectorId
    )
    {
        var branch = new Branch
        {
            Name = "Comercio San Jose",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Commercial,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Supplier> CreateSupplierAsync(AppDbContext context)
    {
        var supplier = new Supplier
        {
            Name = "Proveedor Test",
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    private static async Task<Product> CreateProductAsync(
        AppDbContext context,
        int createdByUserId,
        ProductStatus status = ProductStatus.Validated,
        string sku = "SKU-1"
    )
    {
        var product = new Product
        {
            Name = "Cemento",
            Sku = sku,
            UnitPrice = 1000m,
            UnitCost = 700m,
            Status = status,
            ActiveStatus = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static CreateProductIncorporationTicketDto BuildDto(
        int branchId,
        int productId,
        int supplierId,
        decimal quantity = 10,
        decimal unitCost = 500
    ) =>
        new()
        {
            BranchId = branchId,
            ProductId = productId,
            SupplierId = supplierId,
            Quantity = quantity,
            UnitCost = unitCost,
        };

    [Fact]
    public async Task CreateAsync_BranchNotCommercialOrWarehouse_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "pitoffice@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var officeBranch = new Branch
        {
            Name = "Oficina Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(officeBranch);
        await context.SaveChangesAsync();
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, user.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var act = () =>
            service.CreateAsync(
                BuildDto(officeBranch.Id, product.Id, supplier.Id),
                user.Id,
                new[] { "GeneralManager" }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_WarehouseBranch_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "pitwh@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouseBranch = new Branch
        {
            Name = "Bodega Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(warehouseBranch);
        await context.SaveChangesAsync();
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, user.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var result = await service.CreateAsync(
            BuildDto(warehouseBranch.Id, product.Id, supplier.Id),
            user.Id,
            new[] { "GeneralManager" }
        );

        result.Status.Should().Be(ProductIncorporationTicketStatus.Validated);
    }

    [Fact]
    public async Task CreateAsync_ByGeneralManager_ValidatesImmediatelyAndCreatesInventoryRow()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, user.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var result = await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id, 10),
            user.Id,
            new[] { "GeneralManager" }
        );

        result.Status.Should().Be(ProductIncorporationTicketStatus.Validated);
        var inventory = await context.BranchInventories.FirstAsync(i =>
            i.BranchId == branch.Id && i.ProductId == product.Id
        );
        inventory.Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task CreateAsync_ByBranchAdmin_ValidatesImmediately()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "do@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var branchAdmin = await TestUserFactory.CreateAsync(
            context,
            "ba@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var branch = await CreateBranchAsync(context, director.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, director.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var result = await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id),
            branchAdmin.Id,
            new[] { "BranchAdmin" }
        );

        result.Status.Should().Be(ProductIncorporationTicketStatus.Validated);
    }

    [Fact]
    public async Task CreateAsync_ByBusinessManager_StaysPendingAndDoesNotTouchInventory()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "do2@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var businessManager = await TestUserFactory.CreateAsync(
            context,
            "bm@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var branch = await CreateBranchAsync(context, director.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, director.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var result = await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id),
            businessManager.Id,
            new[] { "BusinessManager" }
        );

        result.Status.Should().Be(ProductIncorporationTicketStatus.PendingValidation);
        (
            await context.BranchInventories.AnyAsync(i =>
                i.BranchId == branch.Id && i.ProductId == product.Id
            )
        )
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ProductNotValidated_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm2@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, user.Id, ProductStatus.PendingValidation);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var act = () =>
            service.CreateAsync(
                BuildDto(branch.Id, product.Id, supplier.Id),
                user.Id,
                new[] { "GeneralManager" }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ValidateAsync_Approve_IncrementsExistingInventoryRow()
    {
        using var context = TestDbContextFactory.Create();
        var manager = await TestUserFactory.CreateAsync(
            context,
            "gm3@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var businessManager = await TestUserFactory.CreateAsync(
            context,
            "bm2@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var branch = await CreateBranchAsync(context, manager.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, manager.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id, 5),
            manager.Id,
            new[] { "GeneralManager" }
        );
        var second = await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id, 3),
            businessManager.Id,
            new[] { "BusinessManager" }
        );

        var validated = await service.ValidateAsync(
            second.Id,
            true,
            manager.Id,
            new[] { "GeneralManager" }
        );

        validated.Status.Should().Be(ProductIncorporationTicketStatus.Validated);
        var rows = await context
            .BranchInventories.Where(i => i.BranchId == branch.Id && i.ProductId == product.Id)
            .ToListAsync();
        rows.Should().HaveCount(1);
        rows[0].Quantity.Should().Be(8m);
    }

    [Fact]
    public async Task ValidateAsync_Reject_DoesNotTouchInventory()
    {
        using var context = TestDbContextFactory.Create();
        var manager = await TestUserFactory.CreateAsync(
            context,
            "gm4@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var businessManager = await TestUserFactory.CreateAsync(
            context,
            "bm3@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var branch = await CreateBranchAsync(context, manager.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, manager.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var ticket = await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id, 5),
            businessManager.Id,
            new[] { "BusinessManager" }
        );

        var rejected = await service.ValidateAsync(
            ticket.Id,
            false,
            manager.Id,
            new[] { "GeneralManager" }
        );

        rejected.Status.Should().Be(ProductIncorporationTicketStatus.Rejected);
        (
            await context.BranchInventories.AnyAsync(i =>
                i.BranchId == branch.Id && i.ProductId == product.Id
            )
        )
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_OnValidatedTicket_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var manager = await TestUserFactory.CreateAsync(
            context,
            "gm5@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, manager.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, manager.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);
        var ticket = await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id),
            manager.Id,
            new[] { "GeneralManager" }
        );

        var act = () => service.DeleteAsync(ticket.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_OnPendingTicket_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var manager = await TestUserFactory.CreateAsync(
            context,
            "gm6@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var businessManager = await TestUserFactory.CreateAsync(
            context,
            "bm4@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var branch = await CreateBranchAsync(context, manager.Id);
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, manager.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);
        var ticket = await service.CreateAsync(
            BuildDto(branch.Id, product.Id, supplier.Id),
            businessManager.Id,
            new[] { "BusinessManager" }
        );

        await service.DeleteAsync(ticket.Id);

        (await context.ProductIncorporationTickets.FindAsync(ticket.Id)).Should().BeNull();
    }
}
