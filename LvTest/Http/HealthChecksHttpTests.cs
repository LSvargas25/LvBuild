using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace LvTest.Http;

public class HealthChecksHttpTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_WithoutToken_ReturnHealthy(string path)
    {
        using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Ready_IncludesTheDatabaseCheck_LivenessDoesNot()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();

        using var live = JsonDocument.Parse(await client.GetStringAsync("/health"));
        using var ready = JsonDocument.Parse(await client.GetStringAsync("/health/ready"));

        live.RootElement.GetProperty("checks").GetArrayLength().Should().Be(0);
        ready
            .RootElement.GetProperty("checks")
            .EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .Should()
            .Equal("database");
    }
}
