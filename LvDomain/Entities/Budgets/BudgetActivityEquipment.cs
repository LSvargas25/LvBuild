using LvDomain.Common;

namespace LvDomain.Entities.Budgets;

public class BudgetActivityEquipment : BaseEntity
{
    public int ActivityId { get; set; }
    public BudgetActivity Activity { get; set; } = null!;

    public string EquipmentName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
