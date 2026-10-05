using System.Text;
using LvApi.Configuration;

namespace LvApi.Extensions;

public static class OptionsExtensions
{
    /// <summary>
    /// Binds every configuration section the API uses and validates the required ones when the
    /// host starts, so a missing secret fails fast with a message naming the variable to set.
    /// </summary>
    public static IServiceCollection AddAppOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Key),
                "Jwt:Key is not configured. Set the Jwt__Key environment variable (or the "
                    + "'Jwt:Key' user-secret) to a random string of at least 32 characters."
            )
            .Validate(
                o => string.IsNullOrWhiteSpace(o.Key) || Encoding.UTF8.GetByteCount(o.Key) >= 32,
                "Jwt:Key is too short: HMAC-SHA256 needs at least 32 bytes (32 ASCII characters)."
            )
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Issuer),
                "Jwt:Issuer is not configured (Jwt__Issuer)."
            )
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Audience),
                "Jwt:Audience is not configured (Jwt__Audience)."
            )
            .Validate(
                o => o.AccessTokenExpirationHours > 0 && o.RefreshTokenExpirationHours > 0,
                "Jwt:AccessTokenExpirationHours and Jwt:RefreshTokenExpirationHours must be positive."
            )
            .ValidateOnStart();

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Configure(o =>
                o.ConnectionString =
                    configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
                    ?? string.Empty
            )
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                "ConnectionStrings:DefaultConnection is not configured. Set the "
                    + "ConnectionStrings__DefaultConnection environment variable, e.g. "
                    + "'Host=...;Database=...;Username=...;Password=...;SSL Mode=Require'."
            )
            .ValidateOnStart();

        services
            .AddOptions<CorsOriginsOptions>()
            .Bind(configuration.GetSection(CorsOriginsOptions.SectionName));
        services
            .AddOptions<DemoSeedOptions>()
            .Bind(configuration.GetSection(DemoSeedOptions.SectionName));
        services
            .AddOptions<ApiDocsOptions>()
            .Bind(configuration.GetSection(ApiDocsOptions.SectionName));
        services
            .AddOptions<ReverseProxyOptions>()
            .Bind(configuration.GetSection(ReverseProxyOptions.SectionName));

        return services;
    }
}
