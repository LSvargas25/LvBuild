using LvApi.Extensions;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LvTest.Common;

/// <summary>
/// The application's real DI registrations (AddApplicationServices + AddInfrastructureServices)
/// with the DbContext swapped for the given provider. Used where a test needs many services
/// wired exactly as in production (e.g. the demo seeder).
/// </summary>
public static class AppServicesFactory
{
    public static ServiceProvider Create(Action<DbContextOptionsBuilder> configureDatabase)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TestConfigurationFactory.Create());
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());

        services.AddApplicationServices();
        services.AddInfrastructureServices();

        // Same reasoning as ApiWebApplicationFactory: drop every registration generic over
        // AppDbContext so only the test provider is configured.
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
        foreach (var descriptor in toRemove)
            services.Remove(descriptor);

        services.AddDbContext<AppDbContext>(configureDatabase);

        return services.BuildServiceProvider(validateScopes: true);
    }

    public static ServiceProvider CreateInMemory()
    {
        // One database name for the whole provider, so every scope sees the same data.
        var databaseName = Guid.NewGuid().ToString();
        return Create(options => options.UseInMemoryDatabase(databaseName));
    }
}
