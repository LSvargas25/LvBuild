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
}
