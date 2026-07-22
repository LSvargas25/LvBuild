using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Projects;
using LvDomain.Enums;

namespace LvDomain.Entities.SiteLogs;

public class SiteLog : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }

    public string TaskDescription { get; set; } = string.Empty;
    public string? PendingTasks { get; set; }

    public decimal TotalPayroll { get; set; }
    public decimal TotalMaterials { get; set; }
    public decimal? ProgressPercentage { get; set; }

    public SiteLogStatus Status { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public int? ChapterId { get; set; }
    public BudgetChapter? Chapter { get; set; }

    public ICollection<SiteLogWorker> Workers { get; set; } = new List<SiteLogWorker>();
    public ICollection<SiteLogMaterial> Materials { get; set; } = new List<SiteLogMaterial>();
    public ICollection<SiteLogEquipment> Equipment { get; set; } = new List<SiteLogEquipment>();
}
