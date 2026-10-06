using FluentAssertions;
using LvInfrastructure.Persistence;
using LvInfrastructure.Seeding;
using LvTest.Common;
using LvTest.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LvTest.Integration;

/// <summary>
/// The demo seed on a freshly migrated PostgreSQL database, as Render runs it
/// (Database:MigrateOnStartup + Seed:Demo): real constraints, filtered unique indexes,
/// timestamptz/date columns and the transaction. Running it twice must not duplicate data.
/// </summary>
[Collection(PostgresIntegrationDefinition.Name)]
public class DemoDataSeederPostgresTests
{
    private readonly PostgresFixture _db;

    public DemoDataSeederPostgresTests(PostgresFixture db)
    {
        _db = db;
    }

    [Fact]
    public async Task SeedAsync_OnMigratedPostgres_SucceedsAndIsIdempotent()
    {
        var connectionString = _db.ConnectionStringFor($"demo_{Guid.NewGuid():N}");
        await using var provider = AppServicesFactory.Create(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()
        );

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        }

        try
        {
            (await SeedAsync(provider)).Should().BeTrue();
            var afterFirstRun = await DemoDataSeederTests.CountRowsAsync(provider);

            (await SeedAsync(provider)).Should().BeFalse();
            var afterSecondRun = await DemoDataSeederTests.CountRowsAsync(provider);

            afterSecondRun.Should().Equal(afterFirstRun);
            afterFirstRun.Values.Should().OnlyContain(count => count > 0);
        }
        finally
        {
            await using var scope = provider.CreateAsyncScope();
            await scope
                .ServiceProvider.GetRequiredService<AppDbContext>()
                .Database.EnsureDeletedAsync();
        }
    }

    private static async Task<bool> SeedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }
}
