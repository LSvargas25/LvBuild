using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Projects;
using LvDomain.Entities.SiteLogs;
using LvDomain.Enums;

namespace LvDomain.Entities.Payroll;

public class Payroll : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int SiteLogId { get; set; }
    public SiteLog SiteLog { get; set; } = null!;

    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }

    public decimal TotalPayroll { get; set; }

    public PayrollStatus Status { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public ICollection<PayrollDetail> Details { get; set; } = new List<PayrollDetail>();
}
