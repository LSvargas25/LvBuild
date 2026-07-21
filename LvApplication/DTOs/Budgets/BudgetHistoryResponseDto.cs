using LvDomain.Enums;

namespace LvApplication.DTOs.Budgets;

public class BudgetHistoryResponseDto
{
    public int Id { get; set; }
    public int BudgetId { get; set; }
    public int UserId { get; set; }
    public BudgetStatus? PreviousStatus { get; set; }
    public BudgetStatus NewStatus { get; set; }
    public string? Comment { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}
