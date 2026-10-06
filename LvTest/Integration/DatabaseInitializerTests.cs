using FluentAssertions;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LvTest.Integration;

/// <summary>
/// Database:MigrateOnStartup path: migrating an empty PostgreSQL database creates the whole
/// schema with its seed data, and running it again is a no-op.
/// </summary>
[Collection(PostgresIntegrationDefinition.Name)]
public class DatabaseInitializerTests
{
    private readonly PostgresFixture _db;

    public DatabaseInitializerTests(PostgresFixture db)
    {
        _db = db;
    }

    [Fact]
    public async Task MigrateAsync_OnEmptyDatabase_AppliesAllMigrations_AndIsIdempotent()
    {
        var databaseName = $"startup_{Guid.NewGuid():N}";
        await using var context = _db.CreateContext(databaseName);
        try
        {
            var initializer = new DatabaseInitializer(
                context,
                NullLogger<DatabaseInitializer>.Instance
            );

            await initializer.MigrateAsync();
            await initializer.MigrateAsync();

            (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
            (await context.Database.GetAppliedMigrationsAsync())
                .Should()
                .Equal(context.Database.GetMigrations());
            (await context.Roles.CountAsync()).Should().Be(5);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
