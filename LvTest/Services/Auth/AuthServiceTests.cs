using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Auth;
using LvDomain.Entities.Auth;
using LvDomain.Enums;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Auth;

public class AuthServiceTests
{
    private const string Password = "Password#123";

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokensAndUpdatesUser()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "login-ok@example.com", Password, failedLoginAttempts: 3);
        var authService = ServiceFactory.CreateAuthService(context);

        var result = await authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = Password });

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(0);
        updatedUser.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_IncrementsFailedAttemptsAndDoesNotReturnTokens()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "login-badpwd@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () => await authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "WrongPassword#1" });

        await act.Should().ThrowAsync<ForbiddenException>();

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword5Times_BlocksUser()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "login-blocked@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        for (var i = 0; i < 5; i++)
        {
            var act = async () => await authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "WrongPassword#1" });
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.Status.Should().Be(UserStatus.Blocked);
        updatedUser.BlockedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_UserAlreadyBlocked_ThrowsEvenWithCorrectPassword()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "login-alreadyblocked@example.com", Password, status: UserStatus.Blocked);
        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () => await authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = Password });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokensAndRevokesOldOne()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "refresh-ok@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var loginResult = await authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = Password });

        var refreshResult = await authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = loginResult.RefreshToken });

        refreshResult.RefreshToken.Should().NotBe(loginResult.RefreshToken);
        refreshResult.AccessToken.Should().NotBeNullOrWhiteSpace();

        var oldToken = await context.RefreshTokens.FirstAsync(rt => rt.Token == loginResult.RefreshToken);
        oldToken.Revoked.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "refresh-revoked@example.com", Password);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(10),
            Revoked = true,
            CreatedAt = DateTime.UtcNow
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () => await authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = refreshToken.Token });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "refresh-expired@example.com", Password);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            Revoked = false,
            CreatedAt = DateTime.UtcNow.AddHours(-11)
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () => await authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = refreshToken.Token });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ForgotPassword_Then_ResetPassword_FullFlow_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "forgot-reset@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var loginResult = await authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = Password });

        await authService.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = user.Email });

        var resetToken = await context.PasswordResetTokens.FirstAsync(t => t.UserId == user.Id);

        const string newPassword = "NewPassword#456";
        await authService.ResetPasswordAsync(new ResetPasswordRequestDto { Token = resetToken.Token, NewPassword = newPassword });

        var updatedResetToken = await context.PasswordResetTokens.FindAsync(resetToken.Id);
        updatedResetToken!.Used.Should().BeTrue();

        var previousRefreshToken = await context.RefreshTokens.FirstAsync(rt => rt.Token == loginResult.RefreshToken);
        previousRefreshToken.Revoked.Should().BeTrue();

        var newLogin = await authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = newPassword });
        newLogin.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithUsedToken_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "reset-used@example.com", Password);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            Used = true,
            CreatedAt = DateTime.UtcNow
        };
        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () => await authService.ResetPasswordAsync(new ResetPasswordRequestDto { Token = resetToken.Token, NewPassword = "AnotherPass#1" });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "reset-expired@example.com", Password);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            Used = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-31)
        };
        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () => await authService.ResetPasswordAsync(new ResetPasswordRequestDto { Token = resetToken.Token, NewPassword = "AnotherPass#1" });

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
