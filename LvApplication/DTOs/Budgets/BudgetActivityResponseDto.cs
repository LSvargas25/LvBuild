namespace LvApplication.DTOs.Budgets;

public class BudgetActivityResponseDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal MaterialQuantity { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal EquipmentCost { get; set; }
    public decimal TotalActivity { get; set; }
    public List<BudgetActivityMaterialResponseDto> Materials { get; set; } = new();
    public List<BudgetActivityEquipmentResponseDto> Equipment { get; set; } = new();
    public List<BudgetActivityLaborResponseDto> Labor { get; set; } = new();
}
