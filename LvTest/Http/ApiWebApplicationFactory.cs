using LvInfrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LvTest.Http;

/// <summary>
/// Boots the real LvApi host (real Program.cs, real middleware pipeline, real JWT
/// auth/authorization, real [Authorize(Roles=...)] enforcement) for HTTP-level tests,
/// swapping only the database for an isolated InMemory instance per factory and
/// supplying config values that Program.cs requires at startup (Jwt:Key, connection
/// string) but that must not come from the real appsettings/user-secrets.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "integration-test-signing-key-please-ignore-1234567890";
    public const string TestJwtIssuer = "LvConstruccionesApi";
    public const string TestJwtAudience = "LvConstruccionesClient";

    private readonly string _databaseName = Guid.NewGuid().ToString();

    static ApiWebApplicationFactory()
    {
        // Program.cs reads ConnectionStrings:DefaultConnection and Jwt:Key directly off
        // builder.Configuration and throws eagerly if missing, *before* WebApplicationFactory's
        // ConfigureAppConfiguration/ConfigureServices hooks get a chance to run (those only
        // apply once WebApplicationFactory intercepts the host at Build()). Environment
        // variables, unlike those hooks, are read synchronously by WebApplication.CreateBuilder
        // itself, so this is the only override point that actually lands in time - and it
        // mirrors exactly how production is meant to supply these values anyway.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=unused;Username=unused;Password=unused"
        );
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Removing only DbContextOptions<AppDbContext> isn't enough: EF Core composes
            // every registered IDbContextOptionsConfiguration<AppDbContext> (one per
            // AddDbContext call) onto the same options instance rather than replacing it,
            // so the original UseSqlServer configuration from Program.cs would still apply
            // alongside UseInMemoryDatabase below and EF throws on the two providers.
            // Stripping every descriptor generic over AppDbContext clears all of that.
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext)
                    || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || (
                        d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext))
                    )
                )
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
            );
        });
    }
}
