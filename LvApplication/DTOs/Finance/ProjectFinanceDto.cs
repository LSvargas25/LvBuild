using LvDomain.Enums;

namespace LvApplication.DTOs.Finance;

public class ProjectFinanceDto
{
    public int ProjectId { get; set; }
    public FinancePeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal CurrentDirectExpenses { get; set; }
    public decimal PendingExpenses { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public List<ProjectFinanceMaterialDto> Materials { get; set; } = new();
}
