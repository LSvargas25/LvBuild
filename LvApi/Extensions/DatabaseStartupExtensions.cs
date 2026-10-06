using LvApi.Configuration;
using LvInfrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace LvApi.Extensions;

public static partial class DatabaseStartupExtensions
{
    /// <summary>
    /// Runs the opt-in startup database tasks before the app starts serving requests.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var logger = app
            .Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseStartup");
        var database = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (!database.MigrateOnStartup)
        {
            LogMigrationsSkipped(logger);
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().MigrateAsync();
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Database:MigrateOnStartup is false; not applying migrations at startup."
    )]
    private static partial void LogMigrationsSkipped(ILogger logger);
}
