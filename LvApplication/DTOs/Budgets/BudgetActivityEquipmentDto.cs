namespace LvApplication.DTOs.Budgets;

public class BudgetActivityEquipmentDto
{
    public int? Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
