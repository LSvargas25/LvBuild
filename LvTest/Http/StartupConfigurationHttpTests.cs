using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LvTest.Http;

/// <summary>
/// The API must refuse to start, with a message that names the variable to set, when a
/// required secret is missing (options validated with ValidateOnStart).
/// </summary>
public class StartupConfigurationHttpTests
{
    [Theory]
    [InlineData("Jwt:Key", "", "Jwt__Key")]
    [InlineData("Jwt:Key", "too-short", "at least 32 bytes")]
    [InlineData("ConnectionStrings:DefaultConnection", "", "ConnectionStrings__DefaultConnection")]
    public void Startup_WithMissingOrInvalidSetting_FailsWithActionableMessage(
        string key,
        string value,
        string expectedMessagePart
    )
    {
        using var factory = new ApiWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(
                (_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            )
        );

        var act = () => factory.CreateClient();

        act.Should()
            .Throw<OptionsValidationException>()
            .Which.Message.Should()
            .Contain(expectedMessagePart);
    }
}
