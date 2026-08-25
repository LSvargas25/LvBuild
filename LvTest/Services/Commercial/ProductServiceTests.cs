using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Commercial;
using LvDomain.Enums;
using LvTest.Common;
using Xunit;

namespace LvTest.Services.Commercial;

public class ProductServiceTests
{
    private static CreateProductDto BuildCreateDto(string sku = "SKU-001") =>
        new()
        {
            Name = "Cemento gris 50kg",
            Sku = sku,
            UnitOfMeasure = "Saco",
            UnitPrice = 6500m,
            UnitCost = 5000m,
            Category = "Materiales",
        };

    private static UpdateProductDto BuildUpdateDto() =>
        new()
        {
            Name = "Cemento gris 50kg (actualizado)",
            UnitOfMeasure = "Saco",
            UnitPrice = 7000m,
            UnitCost = 5200m,
            Category = "Materiales",
        };

    [Fact]
    public async Task CreateAsync_ByGeneralManager_AutoValidates()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);

        var result = await service.CreateAsync(
            BuildCreateDto(),
            user.Id,
            new[] { "GeneralManager" }
        );

        result.Status.Should().Be(ProductStatus.Validated);
        result.ValidatedByUserId.Should().Be(user.Id);
        result.ValidatedDate.Should().NotBeNull();
        result.ActiveStatus.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ByOperationsDirector_AutoValidates()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "do@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateProductService(context);

        var result = await service.CreateAsync(
            BuildCreateDto(),
            user.Id,
            new[] { "OperationsDirector" }
        );

        result.Status.Should().Be(ProductStatus.Validated);
    }

    [Fact]
    public async Task CreateAsync_ByBranchAdmin_AutoValidates()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "ba@example.com",
            roleId: TestUserFactory.BranchAdminRoleId
        );
        var service = ServiceFactory.CreateProductService(context);

        var result = await service.CreateAsync(BuildCreateDto(), user.Id, new[] { "BranchAdmin" });

        result.Status.Should().Be(ProductStatus.Validated);
    }

    [Fact]
    public async Task CreateAsync_ByBusinessManager_StaysPending()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "bm@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);

        var result = await service.CreateAsync(
            BuildCreateDto(),
            user.Id,
            new[] { "BusinessManager" }
        );

        result.Status.Should().Be(ProductStatus.PendingValidation);
        result.ValidatedByUserId.Should().BeNull();
        result.ValidatedDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateSku_ThrowsConflict()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm2@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);
        await service.CreateAsync(BuildCreateDto("DUP-1"), user.Id, new[] { "GeneralManager" });

        var act = () =>
            service.CreateAsync(BuildCreateDto("DUP-1"), user.Id, new[] { "GeneralManager" });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ValidateAsync_ApproveByGeneralManager_SetsValidated()
    {
        using var context = TestDbContextFactory.Create();
        var creator = await TestUserFactory.CreateAsync(
            context,
            "bm3@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var validator = await TestUserFactory.CreateAsync(
            context,
            "gm3@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);
        var created = await service.CreateAsync(
            BuildCreateDto("VAL-1"),
            creator.Id,
            new[] { "BusinessManager" }
        );

        var result = await service.ValidateAsync(
            created.Id,
            true,
            validator.Id,
            new[] { "GeneralManager" }
        );

        result.Status.Should().Be(ProductStatus.Validated);
        result.ValidatedByUserId.Should().Be(validator.Id);
    }

    [Fact]
    public async Task ValidateAsync_Reject_SetsRejected()
    {
        using var context = TestDbContextFactory.Create();
        var creator = await TestUserFactory.CreateAsync(
            context,
            "bm4@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var validator = await TestUserFactory.CreateAsync(
            context,
            "gm4@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);
        var created = await service.CreateAsync(
            BuildCreateDto("VAL-2"),
            creator.Id,
            new[] { "BusinessManager" }
        );

        var result = await service.ValidateAsync(
            created.Id,
            false,
            validator.Id,
            new[] { "GeneralManager" }
        );

        result.Status.Should().Be(ProductStatus.Rejected);
    }

    [Fact]
    public async Task ValidateAsync_ByBusinessManager_ThrowsForbidden()
    {
        using var context = TestDbContextFactory.Create();
        var creator = await TestUserFactory.CreateAsync(
            context,
            "bm5@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);
        var created = await service.CreateAsync(
            BuildCreateDto("VAL-3"),
            creator.Id,
            new[] { "BusinessManager" }
        );

        var act = () =>
            service.ValidateAsync(created.Id, true, creator.Id, new[] { "BusinessManager" });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ValidateAsync_AlreadyValidated_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm5@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);
        var created = await service.CreateAsync(
            BuildCreateDto("VAL-4"),
            user.Id,
            new[] { "GeneralManager" }
        );

        var act = () =>
            service.ValidateAsync(created.Id, true, user.Id, new[] { "GeneralManager" });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task UpdateAsync_ByBusinessManager_RevertsValidatedProductToPending()
    {
        using var context = TestDbContextFactory.Create();
        var manager = await TestUserFactory.CreateAsync(
            context,
            "gm6@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var businessManager = await TestUserFactory.CreateAsync(
            context,
            "bm6@example.com",
            roleId: TestUserFactory.BusinessManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);
        var created = await service.CreateAsync(
            BuildCreateDto("UPD-1"),
            manager.Id,
            new[] { "GeneralManager" }
        );
        created.Status.Should().Be(ProductStatus.Validated);

        var result = await service.UpdateAsync(
            created.Id,
            BuildUpdateDto(),
            new[] { "BusinessManager" }
        );

        result.Status.Should().Be(ProductStatus.PendingValidation);
        result.ValidatedByUserId.Should().BeNull();
        result.ValidatedDate.Should().BeNull();
        result.UnitPrice.Should().Be(7000m);
    }

    [Fact]
    public async Task DeactivateAsync_And_ActivateAsync_ToggleActiveStatusOnly()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm7@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateProductService(context);
        var created = await service.CreateAsync(
            BuildCreateDto("ACT-1"),
            user.Id,
            new[] { "GeneralManager" }
        );

        var deactivated = await service.DeactivateAsync(created.Id);
        deactivated.ActiveStatus.Should().BeFalse();
        deactivated.Status.Should().Be(ProductStatus.Validated);

        var reactivated = await service.ActivateAsync(created.Id);
        reactivated.ActiveStatus.Should().BeTrue();
    }
}
