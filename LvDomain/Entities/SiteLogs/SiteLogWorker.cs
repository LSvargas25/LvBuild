using LvDomain.Common;
using LvDomain.Entities.Workers;

namespace LvDomain.Entities.SiteLogs;

public class SiteLogWorker : BaseEntity
{
    public int SiteLogId { get; set; }
    public SiteLog SiteLog { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public decimal HoursWorked { get; set; }
}
