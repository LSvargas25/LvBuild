using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Workers;
using LvDomain.Entities.Branches;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;

namespace LvTest.Services.Workers;

public class WorkerServiceTests
{
    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId, BranchType branchType)
    {
        var branch = new Branch
        {
            Name = "Bodega Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = branchType,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    [Fact]
    public async Task CreateAsync_StorageCategoryAssignedToWarehouseBranch_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir1@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Warehouse);
        var service = ServiceFactory.CreateWorkerService(context);

        var result = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Bodeguero Uno",
            Category = WorkerCategory.Storage,
            Type = WorkerType.WarehouseKeeper,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        result.BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task CreateAsync_NonStorageCategoryAssignedToWarehouseBranch_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir2@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Warehouse);
        var service = ServiceFactory.CreateWorkerService(context);

        var act = async () => await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Vendedor Mal Asignado",
            Category = WorkerCategory.Commercial,
            Type = WorkerType.Salesperson,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("Bodega deben tener categoría Almacenamiento");
    }

    [Fact]
    public async Task CreateAsync_AnyCategoryAssignedToCommercialBranch_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir3@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Commercial);
        var service = ServiceFactory.CreateWorkerService(context);

        var result = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Vendedor Comercio",
            Category = WorkerCategory.Commercial,
            Type = WorkerType.Salesperson,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        result.BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task UpdateAsync_MismatchedCategoryForWarehouseBranch_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir4@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Warehouse);
        var service = ServiceFactory.CreateWorkerService(context);
        var created = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Bodeguero Dos",
            Category = WorkerCategory.Storage,
            Type = WorkerType.WarehouseKeeper,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        var act = async () => await service.UpdateAsync(created.Id, new UpdateWorkerDto
        {
            Name = "Bodeguero Dos",
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Commercial,
            Type = WorkerType.Salesperson,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("Bodega deben tener categoría Almacenamiento");
    }

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

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsWorker()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var created = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Jose Fonseca",
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = 6
        });

        var result = await service.GetByIdAsync(created.Id);

        result.Name.Should().Be("Jose Fonseca");
        result.Category.Should().Be(WorkerCategory.Construction);
        result.Type.Should().Be(WorkerType.Laborer);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var act = async () => await service.GetByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidData_ModifiesFields()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var created = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Ana Solis",
            Category = WorkerCategory.Commercial,
            Type = WorkerType.Salesperson,
            HourlyRate = 9
        });

        var updated = await service.UpdateAsync(created.Id, new UpdateWorkerDto
        {
            Name = "Ana Solis Updated",
            PersonalId = "1-1111-1111",
            PhoneNumber = "8888-1234",
            Status = ActiveStatus.Inactive,
            Category = WorkerCategory.Commercial,
            Type = WorkerType.BusinessManager,
            HourlyRate = 12
        });

        updated.Name.Should().Be("Ana Solis Updated");
        updated.PersonalId.Should().Be("1-1111-1111");
        updated.Status.Should().Be(ActiveStatus.Inactive);
        updated.Type.Should().Be(WorkerType.BusinessManager);
        updated.HourlyRate.Should().Be(12);
    }

    [Fact]
    public async Task UpdateAsync_MismatchedCategoryAndType_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var created = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Luis Vargas",
            Category = WorkerCategory.Office,
            Type = WorkerType.Engineer,
            HourlyRate = 14
        });

        var act = async () => await service.UpdateAsync(created.Id, new UpdateWorkerDto
        {
            Name = "Luis Vargas",
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Office,
            Type = WorkerType.SiteForeman,
            HourlyRate = 14
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("El tipo de trabajador no corresponde a la categoría seleccionada");
    }

    [Fact]
    public async Task UpdateAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var act = async () => await service.UpdateAsync(999, new UpdateWorkerDto
        {
            Name = "Ghost Worker",
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Office,
            Type = WorkerType.Engineer,
            HourlyRate = 10
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesWorker()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var created = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Mario Chinchilla",
            Category = WorkerCategory.Storage,
            Type = WorkerType.Transporter,
            HourlyRate = 7
        });

        await service.DeleteAsync(created.Id);

        var stored = await context.Workers.FindAsync(created.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        var act = async () => await service.DeleteAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_MoreRecordsThanPageSize_PaginatesCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateWorkerService(context);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(new CreateWorkerDto
            {
                Name = $"Worker {i}",
                Category = WorkerCategory.Construction,
                Type = WorkerType.Laborer,
                HourlyRate = 5
            });
        }

        var firstPage = await service.GetAllAsync(pageNumber: 1, pageSize: 2);
        var thirdPage = await service.GetAllAsync(pageNumber: 3, pageSize: 2);

        firstPage.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(5);
        thirdPage.Items.Should().HaveCount(1);
    }
}
