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
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-office@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Main Office",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        result.Id.Should().BeGreaterThan(0);
        result.BranchAdminId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CommercialBranchWithoutAdmin_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-commercial-noadmin@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () =>
            await service.CreateAsync(
                new CreateBranchDto
                {
                    Name = "Downtown Store",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Commercial,
                    OperationsDirectorId = director.Id,
                    BranchAdminId = null,
                }
            );

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception
            .Which.Message.Should()
            .Contain(
                "El administrador de sucursal es obligatorio para sucursales de tipo Comercio o Bodega"
            );
    }

    [Fact]
    public async Task CreateAsync_WarehouseWithoutBranchAdmin_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-warehouse-noadmin@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () =>
            await service.CreateAsync(
                new CreateBranchDto
                {
                    Name = "Central Warehouse",
                    City = "Alajuela",
                    Province = "Alajuela",
                    BranchType = BranchType.Warehouse,
                    OperationsDirectorId = director.Id,
                    BranchAdminId = null,
                }
            );

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception
            .Which.Message.Should()
            .Contain(
                "El administrador de sucursal es obligatorio para sucursales de tipo Comercio o Bodega"
            );
    }

    [Fact]
    public async Task CreateAsync_CommercialWithValidBranchAdmin_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-commercial-ok@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var admin = await TestUserFactory.CreateAsync(
            context,
            "admin-commercial-ok@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Downtown Store",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Commercial,
                OperationsDirectorId = director.Id,
                BranchAdminId = admin.Id,
            }
        );

        result.Id.Should().BeGreaterThan(0);
        result.BranchAdminId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task CreateAsync_WithValidBusinessManager_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-businessmanager-ok@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var admin = await TestUserFactory.CreateAsync(
            context,
            "admin-businessmanager-ok@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var manager = await TestUserFactory.CreateAsync(
            context,
            "manager-businessmanager-ok@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Store With Manager",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Commercial,
                OperationsDirectorId = director.Id,
                BranchAdminId = admin.Id,
                BusinessManagerId = manager.Id,
            }
        );

        result.Id.Should().BeGreaterThan(0);
        result.BusinessManagerId.Should().Be(manager.Id);
    }

    [Fact]
    public async Task CreateAsync_BusinessManagerWithoutCorrectRole_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-businessmanager-badrole@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var admin = await TestUserFactory.CreateAsync(
            context,
            "admin-businessmanager-badrole@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var wrongRoleUser = await TestUserFactory.CreateAsync(
            context,
            "not-a-businessmanager@example.com",
            roleId: TestUserFactory.ProjectAdminRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () =>
            await service.CreateAsync(
                new CreateBranchDto
                {
                    Name = "Store With Bad Manager",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Commercial,
                    OperationsDirectorId = director.Id,
                    BranchAdminId = admin.Id,
                    BusinessManagerId = wrongRoleUser.Id,
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_OperationsDirectorWithoutCorrectRole_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var wrongRoleUser = await TestUserFactory.CreateAsync(
            context,
            "not-a-director@example.com",
            roleId: TestUserFactory.ProjectAdminRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () =>
            await service.CreateAsync(
                new CreateBranchDto
                {
                    Name = "Some Office",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Office,
                    OperationsDirectorId = wrongRoleUser.Id,
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_BranchAdminWithoutCorrectRole_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-wrongadmin@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var wrongRoleUser = await TestUserFactory.CreateAsync(
            context,
            "not-a-branchadmin@example.com",
            roleId: TestUserFactory.ProjectAdminRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () =>
            await service.CreateAsync(
                new CreateBranchDto
                {
                    Name = "Some Store",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Commercial,
                    OperationsDirectorId = director.Id,
                    BranchAdminId = wrongRoleUser.Id,
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_CreatesIndicatorInZero()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-indicator@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Indicator Office",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        result.Indicator.Should().NotBeNull();
        result.Indicator!.Profit.Should().Be(0);
        result.Indicator.Losses.Should().Be(0);
        result.Indicator.DirectExpenses.Should().Be(0);
        result.Indicator.IndirectExpenses.Should().Be(0);
        result.Indicator.TotalWorkers.Should().Be(0);
        result.Indicator.TotalMaterials.Should().Be(0);

        var storedIndicator = await context.BranchIndicators.FirstOrDefaultAsync(bi =>
            bi.BranchId == result.Id
        );
        storedIndicator.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_AsGeneralManager_HardDeletesBranchAndIndicator()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-harddelete@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "To Be Hard Deleted",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        await service.DeleteAsync(created.Id, isGeneralManager: true);

        var storedBranch = await context.Branches.FirstOrDefaultAsync(b => b.Id == created.Id);
        var storedIndicator = await context.BranchIndicators.FirstOrDefaultAsync(bi =>
            bi.BranchId == created.Id
        );

        storedBranch.Should().BeNull();
        storedIndicator.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NotGeneralManager_SoftDeletesToArchived()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-softdelete@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "To Be Archived",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        await service.DeleteAsync(created.Id, isGeneralManager: false);

        var storedBranch = await context.Branches.FirstOrDefaultAsync(b => b.Id == created.Id);

        storedBranch.Should().NotBeNull();
        storedBranch!.Status.Should().Be(BranchStatus.Archived);
    }

    [Fact]
    public async Task GetAllAsync_AsOperationsDirector_OnlyReturnsOwnBranches()
    {
        using var context = TestDbContextFactory.Create();
        var directorA = await TestUserFactory.CreateAsync(
            context,
            "director-a@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var directorB = await TestUserFactory.CreateAsync(
            context,
            "director-b@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "A Office 1",
                City = "SJ",
                Province = "SJ",
                BranchType = BranchType.Office,
                OperationsDirectorId = directorA.Id,
            }
        );
        await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "A Office 2",
                City = "SJ",
                Province = "SJ",
                BranchType = BranchType.Office,
                OperationsDirectorId = directorA.Id,
            }
        );
        await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "B Office 1",
                City = "SJ",
                Province = "SJ",
                BranchType = BranchType.Office,
                OperationsDirectorId = directorB.Id,
            }
        );

        var result = await service.GetAllAsync(1, 10, directorA.Id, new[] { "OperationsDirector" });

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(b => b.OperationsDirectorId == directorA.Id);
    }

    [Fact]
    public async Task GetAllAsync_AsGeneralManager_ReturnsAllBranches()
    {
        using var context = TestDbContextFactory.Create();
        var directorA = await TestUserFactory.CreateAsync(
            context,
            "gm-director-a@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var directorB = await TestUserFactory.CreateAsync(
            context,
            "gm-director-b@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var generalManager = await TestUserFactory.CreateAsync(
            context,
            "gm@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "A Office",
                City = "SJ",
                Province = "SJ",
                BranchType = BranchType.Office,
                OperationsDirectorId = directorA.Id,
            }
        );
        await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "B Office",
                City = "SJ",
                Province = "SJ",
                BranchType = BranchType.Office,
                OperationsDirectorId = directorB.Id,
            }
        );

        var result = await service.GetAllAsync(
            1,
            10,
            generalManager.Id,
            new[] { "GeneralManager" }
        );

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_AsOwningOperationsDirector_ReturnsBranch()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-getbyid-owner@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Owned Office",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        var result = await service.GetByIdAsync(
            created.Id,
            director.Id,
            new[] { "OperationsDirector" }
        );

        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Owned Office");
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var generalManager = await TestUserFactory.CreateAsync(
            context,
            "gm-getbyid-missing@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () =>
            await service.GetByIdAsync(999, generalManager.Id, new[] { "GeneralManager" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_AsNonOwningOperationsDirector_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var owningDirector = await TestUserFactory.CreateAsync(
            context,
            "director-getbyid-owns@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var otherDirector = await TestUserFactory.CreateAsync(
            context,
            "director-getbyid-other@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Not Your Office",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = owningDirector.Id,
            }
        );

        var act = async () =>
            await service.GetByIdAsync(
                created.Id,
                otherDirector.Id,
                new[] { "OperationsDirector" }
            );

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidData_ModifiesFields()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-update@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var admin = await TestUserFactory.CreateAsync(
            context,
            "admin-update@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Original Office",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateBranchDto
            {
                Name = "Renamed Office",
                PhoneNumber = "8888-9999",
                Email = "renamed@example.com",
                City = "Heredia",
                Province = "Heredia",
                BranchType = BranchType.Commercial,
                BranchAdminId = admin.Id,
            }
        );

        updated.Name.Should().Be("Renamed Office");
        updated.City.Should().Be("Heredia");
        updated.BranchType.Should().Be(BranchType.Commercial);
        updated.BranchAdminId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithValidBusinessManager_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-update-businessmanager@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var admin = await TestUserFactory.CreateAsync(
            context,
            "admin-update-businessmanager@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var manager = await TestUserFactory.CreateAsync(
            context,
            "manager-update-businessmanager@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Store Before Manager",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Commercial,
                OperationsDirectorId = director.Id,
                BranchAdminId = admin.Id,
            }
        );

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateBranchDto
            {
                Name = "Store Before Manager",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Commercial,
                BranchAdminId = admin.Id,
                BusinessManagerId = manager.Id,
            }
        );

        updated.BusinessManagerId.Should().Be(manager.Id);
    }

    [Fact]
    public async Task UpdateAsync_BusinessManagerWithoutCorrectRole_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-update-businessmanager-bad@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var admin = await TestUserFactory.CreateAsync(
            context,
            "admin-update-businessmanager-bad@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var wrongRoleUser = await TestUserFactory.CreateAsync(
            context,
            "not-a-businessmanager-update@example.com",
            roleId: TestUserFactory.ProjectAdminRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Store Before Bad Manager",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Commercial,
                OperationsDirectorId = director.Id,
                BranchAdminId = admin.Id,
            }
        );

        var act = async () =>
            await service.UpdateAsync(
                created.Id,
                new UpdateBranchDto
                {
                    Name = "Store Before Bad Manager",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Commercial,
                    BranchAdminId = admin.Id,
                    BusinessManagerId = wrongRoleUser.Id,
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task UpdateAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateBranchService(context);

        var act = async () =>
            await service.UpdateAsync(
                999,
                new UpdateBranchDto
                {
                    Name = "Ghost Office",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Office,
                }
            );

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CommercialWithoutBranchAdmin_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-update-invalid@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Office To Convert",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        var act = async () =>
            await service.UpdateAsync(
                created.Id,
                new UpdateBranchDto
                {
                    Name = "Office To Convert",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Commercial,
                    BranchAdminId = null,
                }
            );

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception
            .Which.Message.Should()
            .Contain(
                "El administrador de sucursal es obligatorio para sucursales de tipo Comercio o Bodega"
            );
    }

    [Fact]
    public async Task ActivateAsync_ReactivatesInactiveBranch()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-activate@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Branch To Reactivate",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        await service.DeactivateAsync(created.Id);

        var result = await service.ActivateAsync(created.Id);

        result.Status.Should().Be(BranchStatus.Active);
    }

    [Fact]
    public async Task DeactivateAsync_SetsStatusToInactive()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-deactivate@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Branch To Deactivate",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = director.Id,
            }
        );

        var result = await service.DeactivateAsync(created.Id);

        result.Status.Should().Be(BranchStatus.Inactive);
    }

    [Fact]
    public async Task GetAllAsync_MoreRecordsThanPageSize_PaginatesCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-pagination@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var generalManager = await TestUserFactory.CreateAsync(
            context,
            "gm-pagination@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(
                new CreateBranchDto
                {
                    Name = $"Office {i}",
                    City = "San Jose",
                    Province = "San Jose",
                    BranchType = BranchType.Office,
                    OperationsDirectorId = director.Id,
                }
            );
        }

        var firstPage = await service.GetAllAsync(
            1,
            2,
            generalManager.Id,
            new[] { "GeneralManager" }
        );
        var thirdPage = await service.GetAllAsync(
            3,
            2,
            generalManager.Id,
            new[] { "GeneralManager" }
        );

        firstPage.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(5);
        thirdPage.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task AssignOperationsDirectorAsync_ChangesDirectorCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var originalDirector = await TestUserFactory.CreateAsync(
            context,
            "original-director@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var newDirector = await TestUserFactory.CreateAsync(
            context,
            "new-director@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateBranchService(context);

        var created = await service.CreateAsync(
            new CreateBranchDto
            {
                Name = "Reassignable Office",
                City = "San Jose",
                Province = "San Jose",
                BranchType = BranchType.Office,
                OperationsDirectorId = originalDirector.Id,
            }
        );

        var result = await service.AssignOperationsDirectorAsync(
            created.Id,
            new AssignOperationsDirectorDto { OperationsDirectorId = newDirector.Id }
        );

        result.OperationsDirectorId.Should().Be(newDirector.Id);

        var storedBranch = await context.Branches.FirstAsync(b => b.Id == created.Id);
        storedBranch.OperationsDirectorId.Should().Be(newDirector.Id);
    }
}
