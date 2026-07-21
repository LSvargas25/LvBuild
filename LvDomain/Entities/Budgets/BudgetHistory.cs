using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Enums;

namespace LvDomain.Entities.Budgets;

public class BudgetHistory : BaseEntity
{
    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public BudgetStatus? PreviousStatus { get; set; }
    public BudgetStatus NewStatus { get; set; }
    public string? Comment { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}
