using FluentAssertions;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Auth;
using LvTest.Common;

namespace LvTest.Services.Auth;

public class SeedDataTests
{
    [Fact]
    public void SeededUsers_HaveNormalizedEmails()
    {
        using var context = TestDbContextFactory.Create();

        // Lookups normalize the incoming email, so a seeded email with uppercase letters
        // could never log in on PostgreSQL (case-sensitive comparison).
        context.Users.Should().NotBeEmpty();
        context
            .Users.ToList()
            .Should()
            .OnlyContain(u => u.Email == EmailNormalizer.Normalize(u.Email));
    }

    [Fact]
    public async Task LoginAsync_SeededAdminWithUppercaseEmail_FindsTheUser()
    {
        using var context = TestDbContextFactory.Create();
        var authService = ServiceFactory.CreateAuthService(context);

        // Wrong password on purpose: the seeded hash is not a known test password. Reaching the
        // password check (failed attempt recorded) proves the user was found by its email.
        var act = async () =>
            await authService.LoginAsync(
                new LoginRequestDto
                {
                    Email = "ADMIN@LVConstrucciones.com",
                    Password = "Not-the-admin-password#1",
                }
            );

        await act.Should().ThrowAsync<ForbiddenException>();
        context.ChangeTracker.Clear();
        context.Users.Single(u => u.Id == 1).FailedLoginAttempts.Should().Be(1);
    }
}
