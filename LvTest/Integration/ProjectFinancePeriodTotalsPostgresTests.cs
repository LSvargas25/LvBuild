using FluentAssertions;
using LvDomain.Enums;
using LvTest.Common;
using Xunit;

namespace LvTest.Integration;

/// <summary>
/// Period expenses of GET /projects/{id}/finance against real PostgreSQL: two months with
/// different costs must show different totals, including the UTC/Costa Rica day boundary.
/// </summary>
[Collection(PostgresIntegrationDefinition.Name)]
public class ProjectFinancePeriodTotalsPostgresTests : IAsyncLifetime
{
    private readonly PostgresFixture _db;

    public ProjectFinancePeriodTotalsPostgresTests(PostgresFixture db)
    {
        _db = db;
    }

    public Task InitializeAsync() => _db.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetFinanceAsync_SeptemberAndOctober_ReturnTheirOwnTotalsAgainstRealPostgres()
    {
        await using var context = _db.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var projectId = await FinancePeriodScenario.BuildAsync(context);
        context.ChangeTracker.Clear();
        var service = ServiceFactory.CreateProjectFinanceService(context);

        var september = await service.GetFinanceAsync(
            projectId,
            FinancePeriod.Month,
            new DateTime(2026, 9, 1)
        );
        var october = await service.GetFinanceAsync(
            projectId,
            FinancePeriod.Month,
            new DateTime(2026, 10, 1)
        );

        september.CurrentDirectExpenses.Should().Be(FinancePeriodScenario.SeptemberDirect);
        september.PayrollExpenses.Should().Be(FinancePeriodScenario.SeptemberPayroll);
        september.PendingExpenses.Should().Be(0);
        october.CurrentDirectExpenses.Should().Be(FinancePeriodScenario.OctoberDirect);
        october.PendingExpenses.Should().Be(FinancePeriodScenario.OctoberPending);
        september.CurrentDirectExpenses.Should().NotBe(october.CurrentDirectExpenses);

        await transaction.RollbackAsync();
    }
}
