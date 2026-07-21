namespace LvApplication.DTOs.Budgets;

public class UpdateBudgetDto
{
    public int CustomerId { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UtilityPercentage { get; set; }
    public decimal IndirectCostsTotal { get; set; }
    public List<BudgetChapterDto> Chapters { get; set; } = new();
}
