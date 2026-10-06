using FluentAssertions;
using LvDomain.Enums;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Finance;

/// <summary>
/// The finance view's expenses belong to the selected period, not to the whole project
/// (ProjectFinancePeriodTotalsPostgresTests repeats this on PostgreSQL).
/// </summary>
public class ProjectFinancePeriodTotalsTests
{
    [Fact]
    public async Task GetFinanceAsync_TwoMonths_EachMonthOnlyCountsItsOwnExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var projectId = await FinancePeriodScenario.BuildAsync(context);
        var service = ServiceFactory.CreateProjectFinanceService(context);

        var september = await service.GetFinanceAsync(
            projectId,
            FinancePeriod.Month,
            new DateTime(2026, 9, 15)
        );
        var october = await service.GetFinanceAsync(
            projectId,
            FinancePeriod.Month,
            new DateTime(2026, 10, 15)
        );

        september.CurrentDirectExpenses.Should().Be(FinancePeriodScenario.SeptemberDirect);
        september.PayrollExpenses.Should().Be(FinancePeriodScenario.SeptemberPayroll);
        september.MaterialExpenses.Should().Be(FinancePeriodScenario.SeptemberMaterials);
        september.IncidentExpenses.Should().Be(FinancePeriodScenario.SeptemberIncidents);
        september.PendingExpenses.Should().Be(0);
        september.Materials.Sum(m => m.Total).Should().Be(september.MaterialExpenses);

        october.CurrentDirectExpenses.Should().Be(FinancePeriodScenario.OctoberDirect);
        october.PayrollExpenses.Should().Be(FinancePeriodScenario.OctoberPayroll);
        october.MaterialExpenses.Should().Be(FinancePeriodScenario.OctoberMaterials);
        october.IncidentExpenses.Should().Be(0);
        october.PendingExpenses.Should().Be(FinancePeriodScenario.OctoberPending);

        // The project's running totals still hold everything.
        var project = await context.Projects.SingleAsync(p => p.Id == projectId);
        project
            .CurrentDirectExpenses.Should()
            .Be(FinancePeriodScenario.SeptemberDirect + FinancePeriodScenario.OctoberDirect);
        project.PendingExpenses.Should().Be(FinancePeriodScenario.OctoberPending);
    }
}
