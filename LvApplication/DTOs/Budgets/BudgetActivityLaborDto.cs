using LvDomain.Enums;

namespace LvApplication.DTOs.Budgets;

public class BudgetActivityLaborDto
{
    public int? Id { get; set; }
    public WorkerType WorkerType { get; set; }
    public decimal HourlyRate { get; set; }
}
