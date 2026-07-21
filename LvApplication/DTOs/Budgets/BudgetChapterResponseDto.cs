namespace LvApplication.DTOs.Budgets;

public class BudgetChapterResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int EstimatedWeeks { get; set; }
    public decimal TotalChapter { get; set; }
    public List<BudgetActivityResponseDto> Activities { get; set; } = new();
}
