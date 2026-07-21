using LvDomain.Common;

namespace LvDomain.Entities.Budgets;

public class BudgetActivity : BaseEntity
{
    public int ChapterId { get; set; }
    public BudgetChapter Chapter { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal MaterialQuantity { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal EquipmentCost { get; set; }
    public decimal TotalActivity { get; set; }

    public ICollection<BudgetActivityMaterial> Materials { get; set; } = new List<BudgetActivityMaterial>();
    public ICollection<BudgetActivityEquipment> Equipment { get; set; } = new List<BudgetActivityEquipment>();
    public ICollection<BudgetActivityLabor> Labor { get; set; } = new List<BudgetActivityLabor>();
}
