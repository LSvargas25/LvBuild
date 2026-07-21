using LvDomain.Common;

namespace LvDomain.Entities.Budgets;

public class BudgetChapter : BaseEntity
{
    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal TotalChapter { get; set; }
    public int EstimatedWeeks { get; set; }

    public ICollection<BudgetActivity> Activities { get; set; } = new List<BudgetActivity>();
}
