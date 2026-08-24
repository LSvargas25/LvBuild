using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LvApplication.DTOs.Auth;
using LvDomain.Entities.Customers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LvTest.Http;

/// <summary>
/// HTTP/controller-level tests: real Program.cs host, real middleware pipeline,
/// real JWT issuance/validation, real [Authorize(Roles=...)] enforcement. Complements
/// the service-level unit tests (which never exercise the auth/authorization layer at all)
/// and the SQL Server integration tests (which never go through HTTP).
/// </summary>
public class CustomersControllerHttpTests
{
    private const string SeedPassword = "Password#123";

    [Fact]
    public async Task GetCustomers_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken_UsableAgainstProtectedProfileEndpoint()
    {
        using var factory = new ApiWebApplicationFactory();
        var email = $"http-login-{Guid.NewGuid():N}@example.com";
        await SeedUserAsync(factory, email, TestUserFactory.ProjectAdminRoleId);
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = SeedPassword
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile!.Email.Should().Be(email);
    }

    [Fact]
    public async Task DeleteCustomer_AsProjectAdmin_ReturnsForbidden_RoleAuthorizationEnforcedOverHttp()
    {
        using var factory = new ApiWebApplicationFactory();
        var email = $"http-forbidden-{Guid.NewGuid():N}@example.com";
        var customerId = await SeedUserAndCustomerAsync(factory, email, TestUserFactory.ProjectAdminRoleId);
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = SeedPassword
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        var deleteResponse = await client.DeleteAsync($"/api/customers/{customerId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCustomer_AsGeneralManager_SoftDeletesOverHttp()
    {
        using var factory = new ApiWebApplicationFactory();
        var email = $"http-allowed-{Guid.NewGuid():N}@example.com";
        var customerId = await SeedUserAndCustomerAsync(factory, email, TestUserFactory.GeneralManagerRoleId);
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = SeedPassword
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        var deleteResponse = await client.DeleteAsync($"/api/customers/{customerId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = factory.Services.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await context.Customers.FindAsync(customerId);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(ActiveStatus.Inactive);
    }

    private static async Task SeedUserAsync(ApiWebApplicationFactory factory, string email, int roleId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        await TestUserFactory.CreateAsync(context, email, SeedPassword, roleId);
    }

    private static async Task<int> SeedUserAndCustomerAsync(ApiWebApplicationFactory factory, string email, int roleId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        await TestUserFactory.CreateAsync(context, email, SeedPassword, roleId);

        var customer = new Customer
        {
            Name = "Cliente HTTP Test",
            CustomerType = CustomerType.Store,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer.Id;
    }
}
