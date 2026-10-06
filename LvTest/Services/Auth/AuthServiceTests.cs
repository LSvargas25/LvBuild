using System.Text;
using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Auth;
using LvApplication.Services.Auth;
using LvDomain.Entities.Auth;
using LvDomain.Enums;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LvTest.Services.Auth;

public class AuthServiceTests
{
    private const string Password = "Password#123";

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokensAndUpdatesUser()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "login-ok@example.com",
            Password,
            failedLoginAttempts: 3
        );
        var authService = ServiceFactory.CreateAuthService(context);

        var result = await authService.LoginAsync(
            new LoginRequestDto { Email = user.Email, Password = Password }
        );

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(0);
        updatedUser.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithEmailInDifferentCase_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        await TestUserFactory.CreateAsync(context, "login-case@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var result = await authService.LoginAsync(
            new LoginRequestDto { Email = "Login-Case@Example.com", Password = Password }
        );

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_IncrementsFailedAttemptsAndDoesNotReturnTokens()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "login-badpwd@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () =>
            await authService.LoginAsync(
                new LoginRequestDto { Email = user.Email, Password = "WrongPassword#1" }
            );

        await act.Should().ThrowAsync<ForbiddenException>();

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword5Times_BlocksUser()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "login-blocked@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        for (var i = 0; i < 5; i++)
        {
            var act = async () =>
                await authService.LoginAsync(
                    new LoginRequestDto { Email = user.Email, Password = "WrongPassword#1" }
                );
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
        var user = await TestUserFactory.CreateAsync(
            context,
            "login-alreadyblocked@example.com",
            Password,
            status: UserStatus.Blocked
        );
        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () =>
            await authService.LoginAsync(
                new LoginRequestDto { Email = user.Email, Password = Password }
            );

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokensAndRevokesOldOne()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "refresh-ok@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var loginResult = await authService.LoginAsync(
            new LoginRequestDto { Email = user.Email, Password = Password }
        );

        var refreshResult = await authService.RefreshTokenAsync(
            new RefreshTokenRequestDto { RefreshToken = loginResult.RefreshToken }
        );

        refreshResult.RefreshToken.Should().NotBe(loginResult.RefreshToken);
        refreshResult.AccessToken.Should().NotBeNullOrWhiteSpace();

        var oldToken = await context.RefreshTokens.FirstAsync(rt =>
            rt.Token == loginResult.RefreshToken
        );
        oldToken.Revoked.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "refresh-revoked@example.com",
            Password
        );

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(10),
            Revoked = true,
            CreatedAt = DateTime.UtcNow,
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () =>
            await authService.RefreshTokenAsync(
                new RefreshTokenRequestDto { RefreshToken = refreshToken.Token }
            );

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "refresh-expired@example.com",
            Password
        );

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            Revoked = false,
            CreatedAt = DateTime.UtcNow.AddHours(-11),
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () =>
            await authService.RefreshTokenAsync(
                new RefreshTokenRequestDto { RefreshToken = refreshToken.Token }
            );

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ForgotPassword_Then_ResetPassword_FullFlow_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "forgot-reset@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var loginResult = await authService.LoginAsync(
            new LoginRequestDto { Email = user.Email, Password = Password }
        );

        await authService.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = user.Email });

        var resetToken = await context.PasswordResetTokens.FirstAsync(t => t.UserId == user.Id);

        const string newPassword = "NewPassword#456";
        await authService.ResetPasswordAsync(
            new ResetPasswordRequestDto { Token = resetToken.Token, NewPassword = newPassword }
        );

        var updatedResetToken = await context.PasswordResetTokens.FindAsync(resetToken.Id);
        updatedResetToken!.Used.Should().BeTrue();

        var previousRefreshToken = await context.RefreshTokens.FirstAsync(rt =>
            rt.Token == loginResult.RefreshToken
        );
        previousRefreshToken.Revoked.Should().BeTrue();

        var newLogin = await authService.LoginAsync(
            new LoginRequestDto { Email = user.Email, Password = newPassword }
        );
        newLogin.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForgotPasswordAsync_InDevelopment_WithExistingEmail_ReturnsResetToken()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "forgot-dev@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(
            context,
            hostEnvironment: new FakeHostEnvironment { EnvironmentName = Environments.Development }
        );

        var response = await authService.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = user.Email }
        );

        var resetToken = await context.PasswordResetTokens.FirstAsync(t => t.UserId == user.Id);
        response.ResetToken.Should().Be(resetToken.Token);
    }

    [Fact]
    public async Task ForgotPasswordAsync_OutsideDevelopment_NeverReturnsResetTokenInResponse()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "forgot-prod@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(
            context,
            hostEnvironment: new FakeHostEnvironment { EnvironmentName = Environments.Production }
        );

        var response = await authService.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = user.Email }
        );

        response.ResetToken.Should().BeNull();

        var resetToken = await context.PasswordResetTokens.SingleAsync(t => t.UserId == user.Id);
        resetToken.Should().NotBeNull();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsGenericResponse_RegardlessOfWhetherEmailExists()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "forgot-exists@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(
            context,
            hostEnvironment: new FakeHostEnvironment { EnvironmentName = Environments.Production }
        );

        var responseForExistingEmail = await authService.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = user.Email }
        );
        var responseForUnknownEmail = await authService.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = "does-not-exist@example.com" }
        );

        responseForExistingEmail.Message.Should().Be(responseForUnknownEmail.Message);
        responseForExistingEmail.ResetToken.Should().BeNull();
        responseForUnknownEmail.ResetToken.Should().BeNull();

        var tokenCountForUnknownEmail = await context.PasswordResetTokens.CountAsync(t =>
            t.UserId != user.Id
        );
        tokenCountForUnknownEmail.Should().Be(0);
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
            CreatedAt = DateTime.UtcNow,
        };
        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () =>
            await authService.ResetPasswordAsync(
                new ResetPasswordRequestDto
                {
                    Token = resetToken.Token,
                    NewPassword = "AnotherPass#1",
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "reset-expired@example.com",
            Password
        );

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            Used = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-31),
        };
        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync();

        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () =>
            await authService.ResetPasswordAsync(
                new ResetPasswordRequestDto
                {
                    Token = resetToken.Token,
                    NewPassword = "AnotherPass#1",
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task GetMyProfileAsync_ReturnsExpectedData()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-get@example.com",
            Password,
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var authService = ServiceFactory.CreateAuthService(context);

        var profile = await authService.GetMyProfileAsync(user.Id);

        profile.Id.Should().Be(user.Id);
        profile.Name.Should().Be(user.Name);
        profile.Email.Should().Be(user.Email);
        profile.Status.Should().Be(user.Status.ToString());
        profile.Roles.Should().ContainSingle().Which.Should().Be("OperationsDirector");
    }

    [Fact]
    public async Task UpdateMyProfileAsync_UpdatesNameOnly()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "me-update@example.com", Password);
        var authService = ServiceFactory.CreateAuthService(context);

        var result = await authService.UpdateMyProfileAsync(
            user.Id,
            new UpdateProfileDto { Name = "Nuevo Nombre" }
        );

        result.Name.Should().Be("Nuevo Nombre");

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.Name.Should().Be("Nuevo Nombre");
        updatedUser.Email.Should().Be(user.Email);
        updatedUser.PasswordHash.Should().Be(user.PasswordHash);
        updatedUser.Status.Should().Be(user.Status);
    }

    [Fact]
    public async Task ChangeMyPasswordAsync_WithCorrectCurrentPassword_UpdatesHashAndRevokesAllRefreshTokens()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-changepwd-ok@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        var loginResult = await authService.LoginAsync(
            new LoginRequestDto { Email = user.Email, Password = Password }
        );

        const string newPassword = "NewPassword#789";
        await authService.ChangeMyPasswordAsync(
            user.Id,
            new ChangePasswordDto { CurrentPassword = Password, NewPassword = newPassword }
        );

        var updatedUser = await context.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify(newPassword, updatedUser!.PasswordHash).Should().BeTrue();

        var oldRefreshToken = await context.RefreshTokens.FirstAsync(rt =>
            rt.Token == loginResult.RefreshToken
        );
        oldRefreshToken.Revoked.Should().BeTrue();

        var act = async () =>
            await authService.RefreshTokenAsync(
                new RefreshTokenRequestDto { RefreshToken = loginResult.RefreshToken }
            );
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ChangeMyPasswordAsync_WithWrongCurrentPassword_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-changepwd-bad@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () =>
            await authService.ChangeMyPasswordAsync(
                user.Id,
                new ChangePasswordDto
                {
                    CurrentPassword = "WrongPassword#1",
                    NewPassword = "NewPassword#789",
                }
            );

        await act.Should().ThrowAsync<ValidationAppException>();

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.PasswordHash.Should().Be(user.PasswordHash);
    }

    [Fact]
    public async Task UpdateMyProfilePhotoAsync_WithInvalidContentType_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-photo-badtype@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        var act = async () =>
            await authService.UpdateMyProfilePhotoAsync(user.Id, stream, "file.txt", "text/plain");

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task UpdateMyProfilePhotoAsync_WithFileOverSizeLimit_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-photo-toobig@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        var oversized = new byte[ProfilePhotoRules.MaxSizeBytes + 1];
        JpegHeader.CopyTo(oversized, 0);
        using var stream = new MemoryStream(oversized);

        var act = async () =>
            await authService.UpdateMyProfilePhotoAsync(user.Id, stream, "photo.jpg", "image/jpeg");

        await act.Should().ThrowAsync<ValidationAppException>().WithMessage("*1 MB*");
        context.StoredFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMyProfilePhotoAsync_DeclaredJpegButContentIsNotAnImage_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-photo-spoof@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<script>alert(1)</script>"));

        var act = async () =>
            await authService.UpdateMyProfilePhotoAsync(user.Id, stream, "photo.jpg", "image/jpeg");

        await act.Should().ThrowAsync<ValidationAppException>();
        context.StoredFiles.Should().BeEmpty();
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public async Task UpdateMyProfilePhotoAsync_ValidPngOrWebp_IsStoredInTheDatabase(
        string contentType
    )
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            $"me-photo-{contentType.Replace('/', '-')}@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);
        var content = contentType == "image/png" ? PngHeader : WebpHeader;

        using var stream = new MemoryStream(content);
        var result = await authService.UpdateMyProfilePhotoAsync(
            user.Id,
            stream,
            "photo",
            contentType
        );

        result.ProfilePhotoUrl.Should().Be($"/api/users/{user.Id}/photo");
        var photo = await authService.GetProfilePhotoAsync(user.Id);
        photo.ContentType.Should().Be(contentType);
        photo.Content.Should().Equal(content);
    }

    [Fact]
    public async Task UpdateMyProfilePhotoAsync_ReplacingExistingPhoto_DeletesPreviousFile()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-photo-replace@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        byte[] first = [.. JpegHeader, 1];
        byte[] second = [.. JpegHeader, 2];
        using (var stream = new MemoryStream(first))
            await authService.UpdateMyProfilePhotoAsync(user.Id, stream, "a.jpg", "image/jpeg");
        using (var stream = new MemoryStream(second))
            await authService.UpdateMyProfilePhotoAsync(user.Id, stream, "b.jpg", "image/jpeg");

        context.StoredFiles.Should().ContainSingle().Which.Content.Should().Equal(second);
        (await authService.GetProfilePhotoAsync(user.Id)).Content.Should().Equal(second);
    }

    [Fact]
    public async Task GetProfilePhotoAsync_UserWithoutPhoto_ThrowsNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "me-photo-none@example.com",
            Password
        );
        var authService = ServiceFactory.CreateAuthService(context);

        var act = async () => await authService.GetProfilePhotoAsync(user.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0];
    private static readonly byte[] WebpHeader = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, 0];
}
