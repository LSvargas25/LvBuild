using FluentAssertions;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;
using LvTest.Common;
using Xunit;

namespace LvTest.Services.Commercial;

public class BranchInventoryServiceTests
{
    private static async Task<Branch> CreateBranchAsync(
        LvInfrastructure.Persistence.AppDbContext context,
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

    private static async Task<Product> CreateValidatedProductAsync(
        LvInfrastructure.Persistence.AppDbContext context,
        int createdByUserId,
        string sku,
        string name = "Producto"
    )
    {
        var product = new Product
        {
            Name = name,
            Sku = sku,
            UnitPrice = 1000m,
            UnitCost = 700m,
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

    [Fact]
    public async Task GetByBranchAsync_ReturnsOnlyRowsForThatBranch_WithProductInfo()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "do@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var branchA = await CreateBranchAsync(context, director.Id);
        var branchB = await CreateBranchAsync(context, director.Id);
        var productX = await CreateValidatedProductAsync(
            context,
            director.Id,
            "SKU-X",
            "Producto X"
        );
        var productY = await CreateValidatedProductAsync(
            context,
            director.Id,
            "SKU-Y",
            "Producto Y"
        );
        var productZ = await CreateValidatedProductAsync(
            context,
            director.Id,
            "SKU-Z",
            "Producto Z"
        );

        context.BranchInventories.Add(
            new BranchInventory
            {
                BranchId = branchA.Id,
                ProductId = productY.Id,
                Quantity = 5,
                MinimumStock = 1,
                CreatedAt = DateTime.UtcNow,
            }
        );
        context.BranchInventories.Add(
            new BranchInventory
            {
                BranchId = branchA.Id,
                ProductId = productX.Id,
                Quantity = 10,
                MinimumStock = 2,
                CreatedAt = DateTime.UtcNow,
            }
        );
        context.BranchInventories.Add(
            new BranchInventory
            {
                BranchId = branchB.Id,
                ProductId = productZ.Id,
                Quantity = 3,
                MinimumStock = 0,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var service = ServiceFactory.CreateBranchInventoryService(context);

        var result = await service.GetByBranchAsync(branchA.Id);

        result.Should().HaveCount(2);
        result.Select(i => i.ProductName).Should().ContainInOrder("Producto X", "Producto Y");
        result.Should().OnlyContain(i => i.BranchId == branchA.Id);
        result.First(i => i.ProductId == productX.Id).Sku.Should().Be("SKU-X");
    }
}
