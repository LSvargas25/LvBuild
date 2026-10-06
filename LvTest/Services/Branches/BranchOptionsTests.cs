using FluentAssertions;
using LvDomain.Entities.Branches;
using LvDomain.Enums;
using LvTest.Common;

namespace LvTest.Services.Branches;

public class BranchOptionsTests
{
    [Fact]
    public async Task GetOptionsAsync_ReturnsOnlyActiveBranches_OrderedByName()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-options@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        context.Branches.AddRange(
            NewBranch("Sucursal Norte", BranchStatus.Active, BranchType.Commercial, director.Id),
            NewBranch("Bodega Vieja", BranchStatus.Inactive, BranchType.Warehouse, director.Id),
            NewBranch("Oficina Central", BranchStatus.Active, BranchType.Office, director.Id),
            NewBranch("Archivo", BranchStatus.Archived, BranchType.Office, director.Id)
        );
        await context.SaveChangesAsync();
        var service = ServiceFactory.CreateBranchService(context);

        var result = await service.GetOptionsAsync();

        result.Select(b => b.Name).Should().Equal("Oficina Central", "Sucursal Norte");
        result[0].BranchType.Should().Be(BranchType.Office);
    }

    private static Branch NewBranch(
        string name,
        BranchStatus status,
        BranchType type,
        int directorId
    ) =>
        new()
        {
            Name = name,
            City = "San José",
            Province = "San José",
            Status = status,
            BranchType = type,
            OperationsDirectorId = directorId,
            CreatedAt = DateTime.UtcNow,
        };
}
