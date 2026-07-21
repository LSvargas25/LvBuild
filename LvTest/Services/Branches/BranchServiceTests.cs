using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Branches;
using LvDomain.Enums;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Branches;

public class BranchServiceTests
{
    [Fact]
    public async Task CreateAsync_OfficeWithoutBranchAdmin_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-office@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.CreateAsync(new CreateBranchDto
        {
            Name = "Main Office",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Office,
            OperationsDirectorId = director.Id
        });

        result.Id.Should().BeGreaterThan(0);
        result.BranchAdminId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CommercialBranchWithoutAdmin_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-commercial-noadmin@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () => await service.CreateAsync(new CreateBranchDto
        {
            Name = "Downtown Store",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Commercial,
            OperationsDirectorId = director.Id,
            BranchAdminId = null
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("El administrador de sucursal es obligatorio para sucursales de tipo Comercio o Bodega");
    }

    [Fact]
    public async Task CreateAsync_WarehouseWithoutBranchAdmin_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-warehouse-noadmin@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () => await service.CreateAsync(new CreateBranchDto
        {
            Name = "Central Warehouse",
            City = "Alajuela",
            Province = "Alajuela",
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = director.Id,
            BranchAdminId = null
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("El administrador de sucursal es obligatorio para sucursales de tipo Comercio o Bodega");
    }

    [Fact]
    public async Task CreateAsync_CommercialWithValidBranchAdmin_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-commercial-ok@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var admin = await TestUserFactory.CreateAsync(context, "admin-commercial-ok@example.com", roleId: TestUserFactory.BranchAdminRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.CreateAsync(new CreateBranchDto
        {
            Name = "Downtown Store",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Commercial,
            OperationsDirectorId = director.Id,
            BranchAdminId = admin.Id
        });

        result.Id.Should().BeGreaterThan(0);
        result.BranchAdminId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task CreateAsync_OperationsDirectorWithoutCorrectRole_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var wrongRoleUser = await TestUserFactory.CreateAsync(context, "not-a-director@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () => await service.CreateAsync(new CreateBranchDto
        {
            Name = "Some Office",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Office,
            OperationsDirectorId = wrongRoleUser.Id
        });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_BranchAdminWithoutCorrectRole_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-wrongadmin@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var wrongRoleUser = await TestUserFactory.CreateAsync(context, "not-a-branchadmin@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () => await service.CreateAsync(new CreateBranchDto
        {
            Name = "Some Store",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Commercial,
            OperationsDirectorId = director.Id,
            BranchAdminId = wrongRoleUser.Id
        });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_CreatesIndicatorInZero()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-indicator@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.CreateAsync(new CreateBranchDto
        {
            Name = "Indicator Office",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Office,
            OperationsDirectorId = director.Id
        });

        result.Indicator.Should().NotBeNull();
        result.Indicator!.Profit.Should().Be(0);
        result.Indicator.Losses.Should().Be(0);
        result.Indicator.DirectExpenses.Should().Be(0);
        result.Indicator.IndirectExpenses.Should().Be(0);
        result.Indicator.TotalWorkers.Should().Be(0);
        result.Indicator.TotalMaterials.Should().Be(0);

        var storedIndicator = await context.BranchIndicators.FirstOrDefaultAsync(bi => bi.BranchId == result.Id);
        storedIndicator.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_AsGeneralManager_HardDeletesBranchAndIndicator()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-harddelete@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(new CreateBranchDto
        {
            Name = "To Be Hard Deleted",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Office,
            OperationsDirectorId = director.Id
        });

        await service.DeleteAsync(created.Id, isGeneralManager: true);

        var storedBranch = await context.Branches.FirstOrDefaultAsync(b => b.Id == created.Id);
        var storedIndicator = await context.BranchIndicators.FirstOrDefaultAsync(bi => bi.BranchId == created.Id);

        storedBranch.Should().BeNull();
        storedIndicator.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NotGeneralManager_SoftDeletesToArchived()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-softdelete@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(new CreateBranchDto
        {
            Name = "To Be Archived",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Office,
            OperationsDirectorId = director.Id
        });

        await service.DeleteAsync(created.Id, isGeneralManager: false);

        var storedBranch = await context.Branches.FirstOrDefaultAsync(b => b.Id == created.Id);

        storedBranch.Should().NotBeNull();
        storedBranch!.Status.Should().Be(BranchStatus.Archived);
    }

    [Fact]
    public async Task GetAllAsync_AsOperationsDirector_OnlyReturnsOwnBranches()
    {
        using var context = TestDbContextFactory.Create();
        var directorA = await TestUserFactory.CreateAsync(context, "director-a@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var directorB = await TestUserFactory.CreateAsync(context, "director-b@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        await service.CreateAsync(new CreateBranchDto { Name = "A Office 1", City = "SJ", Province = "SJ", BranchType = BranchType.Office, OperationsDirectorId = directorA.Id });
        await service.CreateAsync(new CreateBranchDto { Name = "A Office 2", City = "SJ", Province = "SJ", BranchType = BranchType.Office, OperationsDirectorId = directorA.Id });
        await service.CreateAsync(new CreateBranchDto { Name = "B Office 1", City = "SJ", Province = "SJ", BranchType = BranchType.Office, OperationsDirectorId = directorB.Id });

        var result = await service.GetAllAsync(1, 10, directorA.Id, new[] { "OperationsDirector" });

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(b => b.OperationsDirectorId == directorA.Id);
    }

    [Fact]
    public async Task GetAllAsync_AsGeneralManager_ReturnsAllBranches()
    {
        using var context = TestDbContextFactory.Create();
        var directorA = await TestUserFactory.CreateAsync(context, "gm-director-a@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var directorB = await TestUserFactory.CreateAsync(context, "gm-director-b@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var generalManager = await TestUserFactory.CreateAsync(context, "gm@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        await service.CreateAsync(new CreateBranchDto { Name = "A Office", City = "SJ", Province = "SJ", BranchType = BranchType.Office, OperationsDirectorId = directorA.Id });
        await service.CreateAsync(new CreateBranchDto { Name = "B Office", City = "SJ", Province = "SJ", BranchType = BranchType.Office, OperationsDirectorId = directorB.Id });

        var result = await service.GetAllAsync(1, 10, generalManager.Id, new[] { "GeneralManager" });

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task AssignOperationsDirectorAsync_ChangesDirectorCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var originalDirector = await TestUserFactory.CreateAsync(context, "original-director@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var newDirector = await TestUserFactory.CreateAsync(context, "new-director@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(new CreateBranchDto
        {
            Name = "Reassignable Office",
            City = "San Jose",
            Province = "San Jose",
            BranchType = BranchType.Office,
            OperationsDirectorId = originalDirector.Id
        });

        var result = await service.AssignOperationsDirectorAsync(created.Id, new AssignOperationsDirectorDto
        {
            OperationsDirectorId = newDirector.Id
        });

        result.OperationsDirectorId.Should().Be(newDirector.Id);

        var storedBranch = await context.Branches.FirstAsync(b => b.Id == created.Id);
        storedBranch.OperationsDirectorId.Should().Be(newDirector.Id);
    }
}
