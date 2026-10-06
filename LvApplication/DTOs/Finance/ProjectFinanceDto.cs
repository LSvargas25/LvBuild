using LvDomain.Enums;

namespace LvApplication.DTOs.Finance;

public class ProjectFinanceDto
{
    public int ProjectId { get; set; }
    public FinancePeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Direct expenses of the period: payrolls paid (PaidAt), material tickets applied (CreatedAt,
    /// same tickets as <see cref="Materials"/>) and incidents approved (Date) within it.
    /// </summary>
    public decimal CurrentDirectExpenses { get; set; }

    public decimal PayrollExpenses { get; set; }
    public decimal MaterialExpenses { get; set; }
    public decimal IncidentExpenses { get; set; }

    /// <summary>
    /// Pending expenses of the period, same composition as Project.PendingExpenses: material
    /// tickets still in review and incidents not yet approved, dated within the period.
    /// </summary>
    public decimal PendingExpenses { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public List<ProjectFinanceMaterialDto> Materials { get; set; } = new();
}
