using LvDomain.Enums;

namespace LvApplication.DTOs.Budgets;

public class BudgetActivityLaborResponseDto
{
    public int Id { get; set; }
    public WorkerType WorkerType { get; set; }
    public decimal HourlyRate { get; set; }
}
