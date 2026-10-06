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
        var (projectId, unpaidPayrollId) = await FinancePeriodScenario.BuildAsync(context);
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
        september.PendingExpenses.Should().Be(FinancePeriodScenario.SeptemberPending);
        october.CurrentDirectExpenses.Should().Be(FinancePeriodScenario.OctoberDirect);
        october.PendingExpenses.Should().Be(FinancePeriodScenario.OctoberPending);
        september.CurrentDirectExpenses.Should().NotBe(october.CurrentDirectExpenses);

        // Paying the carried-over payroll in October moves it from pending to direct.
        await ServiceFactory.CreatePayrollService(context).MarkAsPaidAsync(unpaidPayrollId);
        (await context.Payrolls.FindAsync(unpaidPayrollId))!.PaidAt = new DateTime(
            2026,
            10,
            20,
            18,
            0,
            0,
            DateTimeKind.Utc
        );
        await context.SaveChangesAsync();
        var octoberAfter = await service.GetFinanceAsync(
            projectId,
            FinancePeriod.Month,
            new DateTime(2026, 10, 1)
        );
        octoberAfter.PendingPayrollExpenses.Should().Be(0);
        octoberAfter
            .CurrentDirectExpenses.Should()
            .Be(FinancePeriodScenario.OctoberDirect + FinancePeriodScenario.UnpaidPayroll);
        (await service.GetFinanceAsync(projectId, FinancePeriod.Month, new DateTime(2026, 9, 1)))
            .PendingPayrollExpenses.Should()
            .Be(FinancePeriodScenario.UnpaidPayroll);

        await transaction.RollbackAsync();
    }
}
