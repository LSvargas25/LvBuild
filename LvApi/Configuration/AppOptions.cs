namespace LvApi.Configuration;

// Strongly typed configuration. Every value can come from appsettings, user-secrets or an
// environment variable (double underscore = section separator, e.g. Jwt__Key). The list of
// variables used in production is documented in .env.example.

/// <summary>Section "Jwt". Key, Issuer and Audience are required.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key; at least 32 bytes.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public double AccessTokenExpirationHours { get; set; } = 10;
    public double RefreshTokenExpirationHours { get; set; } = 10;
}

/// <summary>
/// Section "Database", plus the connection string from ConnectionStrings:DefaultConnection.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "DefaultConnection";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Apply pending EF Core migrations when the app starts.</summary>
    public bool MigrateOnStartup { get; set; }
}

/// <summary>Section "Cors": origins allowed to call the API from a browser.</summary>
public sealed class CorsOriginsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>Section "Seed".</summary>
public sealed class DemoSeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Load the demo company (idempotent) when the app starts.</summary>
    public bool Demo { get; set; }
}

/// <summary>Section "Swagger". Swagger is always on in Development.</summary>
public sealed class ApiDocsOptions
{
    public const string SectionName = "Swagger";

    public bool Enabled { get; set; }
}

/// <summary>Section "ReverseProxy".</summary>
public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    /// <summary>
    /// True when the app runs behind a TLS-terminating proxy (Render): trust
    /// X-Forwarded-For/Proto and let the proxy enforce HTTPS instead of the app.
    /// </summary>
    public bool Enabled { get; set; }
}
