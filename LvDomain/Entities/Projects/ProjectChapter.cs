using LvDomain.Common;
using LvDomain.Entities.Budgets;

namespace LvDomain.Entities.Projects;

public class ProjectChapter : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int ChapterId { get; set; }
    public BudgetChapter Chapter { get; set; } = null!;

    public decimal AssignedSoldTotal { get; set; }
    public decimal ActualCostTotal { get; set; }
    public decimal ChapterProfit { get; set; }
    public int IncidentCount { get; set; }
    public decimal? IncidentPercentage { get; set; }
}
