namespace LvApplication.DTOs.Budgets;

public class BudgetChapterDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int EstimatedWeeks { get; set; }
    public List<BudgetActivityDto> Activities { get; set; } = new();
}
