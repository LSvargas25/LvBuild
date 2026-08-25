using LvDomain.Entities.Auth;
using LvDomain.Enums;
using LvInfrastructure.Persistence;

namespace LvTest.Common;

public static class TestUserFactory
{
    public const int GeneralManagerRoleId = 1;
    public const int OperationsDirectorRoleId = 2;
    public const int ProjectAdminRoleId = 3;
    public const int BranchAdminRoleId = 4;
    public const int BusinessManagerRoleId = 5;

    public static async Task<User> CreateAsync(
        AppDbContext context,
        string email,
        string password = "Password#123",
        int? roleId = null,
        UserStatus status = UserStatus.Active,
        int failedLoginAttempts = 0
    )
    {
        var user = new User
        {
            Name = email,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = status,
            FailedLoginAttempts = failedLoginAttempts,
            CreatedAt = DateTime.UtcNow,
        };

        if (roleId.HasValue)
        {
            user.UserRoles.Add(new UserRole { RoleId = roleId.Value });
        }

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }
}
