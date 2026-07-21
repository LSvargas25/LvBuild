using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Workers;
using LvDomain.Enums;
using LvTest.Common;

namespace LvTest.Services.Workers;

public class WorkerServiceTests
{
    [Fact]
    public async Task CreateAsync_OfficeEngineer_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var result = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Carlos Ramirez",
            Category = WorkerCategory.Office,
            Type = WorkerType.Engineer,
            HourlyRate = 15
        });

        result.Id.Should().BeGreaterThan(0);
        result.Category.Should().Be(WorkerCategory.Office);
        result.Type.Should().Be(WorkerType.Engineer);
    }

    [Fact]
    public async Task CreateAsync_OfficeCategoryWithSiteForemanType_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var act = async () => await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Mismatched Worker",
            Category = WorkerCategory.Office,
            Type = WorkerType.SiteForeman,
            HourlyRate = 10
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("El tipo de trabajador no corresponde a la categoría seleccionada");
    }

    [Fact]
    public async Task CreateAsync_StorageCategoryWithBusinessManagerType_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var act = async () => await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Another Mismatched Worker",
            Category = WorkerCategory.Storage,
            Type = WorkerType.BusinessManager,
            HourlyRate = 10
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("El tipo de trabajador no corresponde a la categoría seleccionada");
    }

    [Fact]
    public async Task CreateAsync_StorageWarehouseKeeper_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var result = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Storage Worker",
            Category = WorkerCategory.Storage,
            Type = WorkerType.WarehouseKeeper,
            HourlyRate = 8
        });

        result.Id.Should().BeGreaterThan(0);
    }
}
