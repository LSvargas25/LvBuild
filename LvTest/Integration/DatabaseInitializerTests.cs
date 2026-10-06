using FluentAssertions;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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

    [Fact]
    public async Task IncludePendingPayrollsMigration_AddsUnpaidPayrollsToExistingProjectsPendingExpenses()
    {
        var databaseName = $"backfill_{Guid.NewGuid():N}";
        await using var context = _db.CreateContext(databaseName);
        try
        {
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("20261006001652_InitialCreate");

            // Data as the previous version left it: the unpaid payroll was not a pending expense.
            var (projectId, _) = await FinancePeriodScenario.BuildAsync(context);
            var project = await context.Projects.SingleAsync(p => p.Id == projectId);
            project.PendingExpenses -= FinancePeriodScenario.UnpaidPayroll;
            await context.SaveChangesAsync();
            var before = project.PendingExpenses;

            await migrator.MigrateAsync();

            context.ChangeTracker.Clear();
            (await context.Projects.SingleAsync(p => p.Id == projectId))
                .PendingExpenses.Should()
                .Be(before + FinancePeriodScenario.UnpaidPayroll)
                .And.Be(FinancePeriodScenario.OctoberPending);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
