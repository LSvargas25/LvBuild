using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Common;

/// <summary>
/// Real SQL Server-backed context for integration tests, as opposed to
/// <see cref="TestDbContextFactory"/> which uses EF Core InMemory.
/// Targets a dedicated local database so it never touches the app's dev/demo data.
/// Points to the same local instance the app itself expects (localhost, Windows auth).
/// </summary>
public static class SqlServerTestDbContextFactory
{
    private const string ConnectionString =
        "Server=localhost;Database=LvConstruccionesDb_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;";

    private static readonly SemaphoreSlim MigrateLock = new(1, 1);
    private static bool _migrated;

    public static async Task<AppDbContext> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention()
            .Options;

        var context = new AppDbContext(options);

        if (!_migrated)
        {
            await MigrateLock.WaitAsync();
            try
            {
                if (!_migrated)
                {
                    await context.Database.MigrateAsync();
                    _migrated = true;
                }
            }
            finally
            {
                MigrateLock.Release();
            }
        }

        return context;
    }
}
