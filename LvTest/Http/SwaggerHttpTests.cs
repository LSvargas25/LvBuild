using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LvTest.Http;

/// <summary>The test host runs in the "Testing" environment, i.e. not Development.</summary>
public class SwaggerHttpTests
{
    [Fact]
    public async Task SwaggerJson_WhenEnabled_HasTitleAndControllerXmlComments()
    {
        using var factory = CreateFactory(swaggerEnabled: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("info")
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("LvBuild API");
        doc.RootElement.GetProperty("paths")
            .GetProperty("/api/auth/login")
            .GetProperty("post")
            .GetProperty("summary")
            .GetString()
            .Should()
            .StartWith("Logs in with email and password");
    }

    [Fact]
    public async Task SwaggerJson_WhenNotEnabledOutsideDevelopment_IsNotServed()
    {
        using var factory = CreateFactory(swaggerEnabled: false);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool swaggerEnabled) =>
        new ApiWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(
                (_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Swagger:Enabled"] = swaggerEnabled ? "true" : "false",
                        }
                    )
            )
        );
}
