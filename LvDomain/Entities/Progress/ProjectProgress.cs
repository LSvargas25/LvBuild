using LvDomain.Common;
using LvDomain.Entities.Projects;
using LvDomain.Entities.SiteLogs;

namespace LvDomain.Entities.Progress;

public class ProjectProgress : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int SiteLogId { get; set; }
    public SiteLog SiteLog { get; set; } = null!;

    public decimal ProgressPercentage { get; set; }
    public DateTime CalculatedAt { get; set; }
}
