using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LvInfrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations at startup. Used on hosts without a pre-deploy hook
/// (Render free plan); enabled only by Database:MigrateOnStartup=true.
/// </summary>
public sealed partial class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var pending = (
            await _context.Database.GetPendingMigrationsAsync(cancellationToken)
        ).ToList();
        if (pending.Count == 0)
        {
            LogUpToDate(_logger);
            return;
        }

        LogApplying(_logger, pending.Count, pending);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogFailed(_logger, ex);
            throw;
        }

        LogApplied(_logger, pending.Count, stopwatch.ElapsedMilliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database schema is up to date.")]
    private static partial void LogUpToDate(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Applying {Count} pending migration(s): {Migrations}"
    )]
    private static partial void LogApplying(
        ILogger logger,
        int count,
        IReadOnlyList<string> migrations
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Applied {Count} migration(s) in {ElapsedMs} ms."
    )]
    private static partial void LogApplied(ILogger logger, int count, long elapsedMs);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Applying database migrations failed; the application will not start."
    )]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
