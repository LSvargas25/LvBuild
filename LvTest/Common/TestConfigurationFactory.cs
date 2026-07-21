using Microsoft.Extensions.Configuration;

namespace LvTest.Common;

/// <summary>
/// Minimal ConfigurationProvider-backed IConfiguration for tests.
/// Avoids adding Microsoft.Extensions.Configuration.Memory as a new package dependency
/// by reusing ConfigurationProvider/ConfigurationRoot, which already ship with the core
/// Microsoft.Extensions.Configuration package referenced transitively via LvApplication.
/// </summary>
internal sealed class TestConfigurationProvider : ConfigurationProvider
{
    public TestConfigurationProvider(IDictionary<string, string?> data)
    {
        Data = data;
    }
}

public static class TestConfigurationFactory
{
    public static IConfiguration Create(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Jwt:Key"] = "test-signing-key-for-unit-tests-0123456789ABCDEF",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:AccessTokenExpirationHours"] = "10",
            ["Jwt:RefreshTokenExpirationHours"] = "10",
            ["Security:MaxFailedLoginAttempts"] = "5",
            ["Security:PasswordResetTokenExpirationMinutes"] = "30"
        };

        if (overrides is not null)
        {
            foreach (var kvp in overrides)
            {
                settings[kvp.Key] = kvp.Value;
            }
        }

        var provider = new TestConfigurationProvider(settings);
        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }
}
