using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LvApplication.DTOs.Auth;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LvTest.Http;

/// <summary>
/// Every API error reaches the client as <c>{ statusCode, code, message }</c> with a Spanish
/// message, whether it comes from a service exception, FluentValidation or JSON model binding.
/// </summary>
public class ErrorResponsesHttpTests
{
    private const string SeedPassword = "Password#123";

    [Fact]
    public async Task NotFound_ReturnsSpanishMessageAndStableCode()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = await LoggedInClientAsync(factory);

        var response = await client.GetAsync("/api/projects/123");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await ReadErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("not_found");
        error.GetProperty("message").GetString().Should().Be("No se encontró el proyecto 123.");
    }

    [Fact]
    public async Task FluentValidationFailure_ReturnsSpanishMessageWithSpanishFieldName()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = await LoggedInClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/customers",
            new { name = "", customerType = "Store" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("validation");
        error.GetProperty("message").GetString().Should().Contain("Nombre").And.Contain("vacío");
    }

    [Fact]
    public async Task MalformedBody_ReturnsSpanishValidationErrorInsteadOfProblemDetails()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = await LoggedInClientAsync(factory);

        var response = await client.PostAsync(
            "/api/customers",
            new StringContent(
                """{ "name": "X", "customerType": 12345678901234567890 }""",
                Encoding.UTF8,
                "application/json"
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("validation");
        error.GetProperty("message").GetString().Should().StartWith("La solicitud tiene");
    }

    [Fact]
    public async Task WrongPassword_ReturnsSpanishMessage()
    {
        using var factory = new ApiWebApplicationFactory();
        var email = $"http-errors-{Guid.NewGuid():N}@example.com";
        await SeedUserAsync(factory, email);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = email, Password = "incorrecta" }
        );

        var error = await ReadErrorAsync(response);
        error.GetProperty("message").GetString().Should().Be("Correo o contraseña incorrectos.");
    }

    private static async Task<JsonElement> ReadErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>());

    private static async Task<HttpClient> LoggedInClientAsync(ApiWebApplicationFactory factory)
    {
        var email = $"http-errors-{Guid.NewGuid():N}@example.com";
        await SeedUserAsync(factory, email);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = email, Password = SeedPassword }
        );
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokens!.AccessToken
        );
        return client;
    }

    private static async Task SeedUserAsync(ApiWebApplicationFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        await TestUserFactory.CreateAsync(
            context,
            email,
            SeedPassword,
            TestUserFactory.GeneralManagerRoleId
        );
    }
}
