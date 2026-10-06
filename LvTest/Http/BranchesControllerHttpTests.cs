using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using LvApplication.DTOs.Auth;
using LvApplication.DTOs.Branches;
using LvDomain.Entities.Branches;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LvTest.Http;

/// <summary>
/// Branch management stays restricted to management, but every role needs the list of
/// branches to fill in forms (a budget's branch, opening a cash register).
/// </summary>
public class BranchesControllerHttpTests
{
    private const string SeedPassword = "Password#123";

    // The API writes enums as strings.
    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task GetOptions_AsProjectAdmin_ReturnsActiveBranches()
    {
        using var factory = new ApiWebApplicationFactory();
        var email = $"http-branches-{Guid.NewGuid():N}@example.com";
        await SeedAsync(factory, email, TestUserFactory.ProjectAdminRoleId);
        var client = await LoginAsync(factory, email);

        var response = await client.GetAsync("/api/branches/options");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var options = await response.Content.ReadFromJsonAsync<List<BranchOptionDto>>(ApiJson);
        options!.Select(o => o.Name).Should().Contain("Sucursal Central");
    }

    [Fact]
    public async Task GetAll_AsProjectAdmin_StaysForbidden()
    {
        using var factory = new ApiWebApplicationFactory();
        var email = $"http-branches-{Guid.NewGuid():N}@example.com";
        await SeedAsync(factory, email, TestUserFactory.ProjectAdminRoleId);
        var client = await LoginAsync(factory, email);

        var response = await client.GetAsync("/api/branches");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOptions_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/branches/options");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task SeedAsync(ApiWebApplicationFactory factory, string email, int roleId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        var user = await TestUserFactory.CreateAsync(context, email, SeedPassword, roleId);
        context.Branches.Add(
            new Branch
            {
                Name = "Sucursal Central",
                City = "San José",
                Province = "San José",
                Status = BranchStatus.Active,
                BranchType = BranchType.Office,
                OperationsDirectorId = user.Id,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();
    }

    private static async Task<HttpClient> LoginAsync(ApiWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = email, Password = SeedPassword }
        );
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
