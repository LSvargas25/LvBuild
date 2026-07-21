namespace LvApplication.DTOs.Budgets;

public class BudgetActivityDto
{
    public int? Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal MaterialQuantity { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal EquipmentCost { get; set; }
    public List<BudgetActivityMaterialDto> Materials { get; set; } = new();
    public List<BudgetActivityEquipmentDto> Equipment { get; set; } = new();
    public List<BudgetActivityLaborDto> Labor { get; set; } = new();
}
