using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LvApplication.DTOs.Auth;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.Extensions.DependencyInjection;

namespace LvTest.Http;

public class ProfilePhotoHttpTests
{
    private const string Password = "Password#123";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    [Fact]
    public async Task UploadThenGetPhoto_RoundTripsTheBytesFromTheDatabase()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();
        await AuthenticateAsync(factory, client, $"photo-{Guid.NewGuid():N}@example.com");

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "me.png");

        var upload = await client.PostAsync("/api/auth/me/photo", form);

        upload.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await upload.Content.ReadFromJsonAsync<UserProfileDto>();
        profile!.ProfilePhotoUrl.Should().Be($"/api/users/{profile.Id}/photo");

        var photo = await client.GetAsync(profile.ProfilePhotoUrl);

        photo.StatusCode.Should().Be(HttpStatusCode.OK);
        photo.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await photo.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
    }

    [Fact]
    public async Task GetPhoto_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new ApiWebApplicationFactory();

        var response = await factory.CreateClient().GetAsync("/api/users/1/photo");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task AuthenticateAsync(
        ApiWebApplicationFactory factory,
        HttpClient client,
        string email
    )
    {
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            await TestUserFactory.CreateAsync(
                context,
                email,
                Password,
                TestUserFactory.ProjectAdminRoleId
            );
        }

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = email, Password = Password }
        );
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokens!.AccessToken
        );
    }
}
