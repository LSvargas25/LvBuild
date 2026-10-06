using LvDomain.Enums;

namespace LvApplication.DTOs.Projects;

public class ProjectDto
{
    public int Id { get; set; }

    /// <summary>Display name of the project: the name of the budget it was sold from.</summary>
    public string Name { get; set; } = string.Empty;
    public int OfferId { get; set; }
    public int BudgetId { get; set; }
    public int CustomerId { get; set; }
    public int BranchId { get; set; }
    public ProjectType ProjectType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WeeksCounter { get; set; }
    public decimal TotalWorkedHours { get; set; }
    public int WorkersUsedCount { get; set; }
    public int MaterialsUsedCount { get; set; }
    public decimal CurrentDirectExpenses { get; set; }
    public decimal PendingExpenses { get; set; }
    public decimal CurrentProfit { get; set; }
    public ProjectStatus Status { get; set; }
    public int CreatedByUserId { get; set; }
    public List<ProjectWorkerDto> Workers { get; set; } = new();
}
