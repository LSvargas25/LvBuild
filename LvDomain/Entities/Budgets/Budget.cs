using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Customers;
using LvDomain.Enums;

namespace LvDomain.Entities.Budgets;

public class Budget : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public BudgetStatus Status { get; set; }
    public decimal UtilityPercentage { get; set; }
    public decimal IndirectCostsTotal { get; set; }
    public decimal TotalBudget { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<BudgetChapter> Chapters { get; set; } = new List<BudgetChapter>();
    public ICollection<BudgetHistory> History { get; set; } = new List<BudgetHistory>();
}
