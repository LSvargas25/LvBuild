using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Customers;
using LvDomain.Enums;
using LvTest.Common;

namespace LvTest.Services.Customers;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidData_PersistsCustomer()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateCustomerService(context);

        var result = await service.CreateAsync(
            new CreateCustomerDto
            {
                Name = "Acme Corp",
                CustomerType = CustomerType.Commercial,
                City = "San Jose",
                PhoneNumber = "8888-0000",
                PersonalId = "1-2345-6789",
                Email = "acme@example.com",
            }
        );

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Acme Corp");
        result.Status.Should().Be(ActiveStatus.Active);

        var stored = await context.Customers.FindAsync(result.Id);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCustomer()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateCustomerService(context);

        var created = await service.CreateAsync(
            new CreateCustomerDto
            {
                Name = "Beta Corp",
                CustomerType = CustomerType.Store,
                City = "Alajuela",
                Email = "beta@example.com",
            }
        );

        var result = await service.GetByIdAsync(created.Id);

        result.Name.Should().Be("Beta Corp");
        result.City.Should().Be("Alajuela");
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateCustomerService(context);

        var act = async () => await service.GetByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidData_ModifiesFields()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateCustomerService(context);

        var created = await service.CreateAsync(
            new CreateCustomerDto
            {
                Name = "Gamma Corp",
                CustomerType = CustomerType.Project,
                City = "Heredia",
            }
        );

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateCustomerDto
            {
                Name = "Gamma Corp Updated",
                CustomerType = CustomerType.Commercial,
                Status = ActiveStatus.Inactive,
                City = "Cartago",
            }
        );

        updated.Name.Should().Be("Gamma Corp Updated");
        updated.CustomerType.Should().Be(CustomerType.Commercial);
        updated.Status.Should().Be(ActiveStatus.Inactive);
        updated.City.Should().Be("Cartago");
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletesCustomer()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateCustomerService(context);

        var created = await service.CreateAsync(
            new CreateCustomerDto
            {
                Name = "Delta Corp",
                CustomerType = CustomerType.Store,
                City = "Limon",
            }
        );

        await service.DeleteAsync(created.Id);

        var stored = await context.Customers.FindAsync(created.Id);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(ActiveStatus.Inactive);
    }

    [Fact]
    public async Task GetAllAsync_MoreRecordsThanPageSize_PaginatesCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateCustomerService(context);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(
                new CreateCustomerDto
                {
                    Name = $"Customer {i}",
                    CustomerType = CustomerType.Store,
                    City = "San Jose",
                }
            );
        }

        var firstPage = await service.GetAllAsync(pageNumber: 1, pageSize: 2);
        var thirdPage = await service.GetAllAsync(pageNumber: 3, pageSize: 2);

        firstPage.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(5);
        thirdPage.Items.Should().HaveCount(1);
    }
}
