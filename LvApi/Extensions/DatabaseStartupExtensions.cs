using LvApi.Configuration;
using LvInfrastructure.Persistence;
using LvInfrastructure.Seeding;
using Microsoft.Extensions.Options;

namespace LvApi.Extensions;

public static partial class DatabaseStartupExtensions
{
    /// <summary>
    /// Runs the opt-in startup database tasks before the app starts serving requests:
    /// migrations (Database:MigrateOnStartup) and then the demo data (Seed:Demo).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var logger = app
            .Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseStartup");
        var database = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var seed = app.Services.GetRequiredService<IOptions<DemoSeedOptions>>().Value;

        if (!database.MigrateOnStartup)
            LogMigrationsSkipped(logger);

        if (!database.MigrateOnStartup && !seed.Demo)
            return;

        await using var scope = app.Services.CreateAsyncScope();

        if (database.MigrateOnStartup)
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().MigrateAsync();

        if (seed.Demo)
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Database:MigrateOnStartup is false; not applying migrations at startup."
    )]
    private static partial void LogMigrationsSkipped(ILogger logger);
}
