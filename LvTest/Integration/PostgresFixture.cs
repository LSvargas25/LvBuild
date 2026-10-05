using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace LvTest.Integration;

/// <summary>
/// One disposable PostgreSQL 16 container (Testcontainers, requires Docker) shared by every
/// test in <see cref="PostgresIntegrationCollection"/>. The real migrations are applied once;
/// <see cref="ResetDatabaseAsync"/> then wipes all data with Respawn before each test while
/// keeping the schema, the migrations history and the seeded role catalog.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(
        "postgres:16-alpine"
    ).Build();

    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                // TestUserFactory relies on the seeded role Ids (1..5).
                TablesToIgnore = [new Table("__EFMigrationsHistory"), new Table("roles")],
            }
        );
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
