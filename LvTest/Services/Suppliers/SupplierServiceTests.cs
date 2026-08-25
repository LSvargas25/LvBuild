using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Suppliers;
using LvDomain.Enums;
using LvTest.Common;

namespace LvTest.Services.Suppliers;

public class SupplierServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidData_PersistsSupplier()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateSupplierService(context);

        var result = await service.CreateAsync(
            new CreateSupplierDto
            {
                Name = "Concrete Supplies Inc",
                City = "San Jose",
                PhoneNumber = "8888-1111",
                PersonalId = "3-101-123456",
                Email = "supplies@example.com",
            }
        );

        result.Id.Should().BeGreaterThan(0);
        result.Status.Should().Be(ActiveStatus.Active);

        var stored = await context.Suppliers.FindAsync(result.Id);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsSupplier()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateSupplierService(context);

        var created = await service.CreateAsync(
            new CreateSupplierDto { Name = "Steel Works", City = "Alajuela" }
        );

        var result = await service.GetByIdAsync(created.Id);

        result.Name.Should().Be("Steel Works");
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateSupplierService(context);

        var act = async () => await service.GetByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidData_ModifiesFields()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateSupplierService(context);

        var created = await service.CreateAsync(
            new CreateSupplierDto { Name = "Wood Supplies", City = "Heredia" }
        );

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateSupplierDto
            {
                Name = "Wood Supplies Updated",
                Status = ActiveStatus.Inactive,
                City = "Cartago",
            }
        );

        updated.Name.Should().Be("Wood Supplies Updated");
        updated.Status.Should().Be(ActiveStatus.Inactive);
        updated.City.Should().Be("Cartago");
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletesSupplier()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateSupplierService(context);

        var created = await service.CreateAsync(
            new CreateSupplierDto { Name = "Glass Supplies", City = "Limon" }
        );

        await service.DeleteAsync(created.Id);

        var stored = await context.Suppliers.FindAsync(created.Id);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(ActiveStatus.Inactive);
    }

    [Fact]
    public async Task GetAllAsync_MoreRecordsThanPageSize_PaginatesCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateSupplierService(context);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(
                new CreateSupplierDto { Name = $"Supplier {i}", City = "San Jose" }
            );
        }

        var firstPage = await service.GetAllAsync(pageNumber: 1, pageSize: 2);
        var thirdPage = await service.GetAllAsync(pageNumber: 3, pageSize: 2);

        firstPage.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(5);
        thirdPage.Items.Should().HaveCount(1);
    }
}
