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
        var (projectId, _) = await FinancePeriodScenario.BuildAsync(context);
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
        september.PendingExpenses.Should().Be(FinancePeriodScenario.SeptemberPending);
        september.PendingPayrollExpenses.Should().Be(FinancePeriodScenario.UnpaidPayroll);
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

    [Fact]
    public async Task GetFinanceAsync_UnpaidPayroll_IsPendingUntilThePeriodItIsPaidIn()
    {
        using var context = TestDbContextFactory.Create();
        var (projectId, unpaidPayrollId) = await FinancePeriodScenario.BuildAsync(context);
        var service = ServiceFactory.CreateProjectFinanceService(context);

        // Unpaid: pending balance at the end of September and still at the end of October.
        (await MonthAsync(service, projectId, 9))
            .PendingPayrollExpenses.Should()
            .Be(FinancePeriodScenario.UnpaidPayroll);
        (await MonthAsync(service, projectId, 10))
            .PendingPayrollExpenses.Should()
            .Be(FinancePeriodScenario.UnpaidPayroll);

        // Paid on 20/10: leaves October's pending balance and becomes October's direct expense.
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

        var september = await MonthAsync(service, projectId, 9);
        var october = await MonthAsync(service, projectId, 10);
        var november = await MonthAsync(service, projectId, 11);

        september.PendingPayrollExpenses.Should().Be(FinancePeriodScenario.UnpaidPayroll);
        september.CurrentDirectExpenses.Should().Be(FinancePeriodScenario.SeptemberDirect);
        october.PendingPayrollExpenses.Should().Be(0);
        october.PendingExpenses.Should().Be(2_600m);
        october
            .CurrentDirectExpenses.Should()
            .Be(FinancePeriodScenario.OctoberDirect + FinancePeriodScenario.UnpaidPayroll);
        november.PendingPayrollExpenses.Should().Be(0);

        var project = await context.Projects.SingleAsync(p => p.Id == projectId);
        project.PendingExpenses.Should().Be(2_600m);
    }

    private static Task<LvApplication.DTOs.Finance.ProjectFinanceDto> MonthAsync(
        LvApplication.Services.Finance.ProjectFinanceService service,
        int projectId,
        int month
    ) => service.GetFinanceAsync(projectId, FinancePeriod.Month, new DateTime(2026, month, 15));
}
