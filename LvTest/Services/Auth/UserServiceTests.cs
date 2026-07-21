using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Auth;
using LvTest.Common;

namespace LvTest.Services.Auth;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsConflictException()
    {
        using var context = TestDbContextFactory.Create();
        var userService = ServiceFactory.CreateUserService(context);

        var first = new CreateUserDto
        {
            Name = "Jane Doe",
            Email = "jane.doe@example.com",
            Password = "Password#123",
            RoleIds = new List<int> { TestUserFactory.ProjectAdminRoleId }
        };

        await userService.CreateUserAsync(first);

        var duplicate = new CreateUserDto
        {
            Name = "Jane Doe Duplicate",
            Email = "jane.doe@example.com",
            Password = "Password#456",
            RoleIds = new List<int> { TestUserFactory.ProjectAdminRoleId }
        };

        var act = async () => await userService.CreateUserAsync(duplicate);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsUser()
    {
        using var context = TestDbContextFactory.Create();
        var userService = ServiceFactory.CreateUserService(context);

        var created = await userService.CreateUserAsync(new CreateUserDto
        {
            Name = "John Smith",
            Email = "john.smith@example.com",
            Password = "Password#123",
            RoleIds = new List<int> { TestUserFactory.ProjectAdminRoleId }
        });

        var result = await userService.GetByIdAsync(created.Id);

        result.Name.Should().Be("John Smith");
        result.Email.Should().Be("john.smith@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var userService = ServiceFactory.CreateUserService(context);

        var act = async () => await userService.GetByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_MoreRecordsThanPageSize_PaginatesCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var userService = ServiceFactory.CreateUserService(context);

        // UserConfiguration seeds one admin user (Id=1) via HasData, present in every fresh context,
        // so only 4 more are created here to land on a round total of 5.
        for (var i = 1; i <= 4; i++)
        {
            await userService.CreateUserAsync(new CreateUserDto
            {
                Name = $"User {i}",
                Email = $"pagination-user-{i}@example.com",
                Password = "Password#123",
                RoleIds = new List<int> { TestUserFactory.ProjectAdminRoleId }
            });
        }

        var firstPage = await userService.GetAllAsync(pageNumber: 1, pageSize: 2);
        var thirdPage = await userService.GetAllAsync(pageNumber: 3, pageSize: 2);

        firstPage.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(5);
        thirdPage.Items.Should().HaveCount(1);
    }
}
