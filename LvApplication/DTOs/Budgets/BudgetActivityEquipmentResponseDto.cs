namespace LvApplication.DTOs.Budgets;

public class BudgetActivityEquipmentResponseDto
{
    public int Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
