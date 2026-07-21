using LvDomain.Common;
using LvDomain.Entities.Materials;

namespace LvDomain.Entities.Budgets;

public class BudgetActivityMaterial : BaseEntity
{
    public int ActivityId { get; set; }
    public BudgetActivity Activity { get; set; } = null!;

    public int MaterialId { get; set; }
    public MaterialCatalog Material { get; set; } = null!;

    public decimal UnitPrice { get; set; }
}
