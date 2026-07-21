using LvDomain.Common;
using LvDomain.Entities.Auth;

namespace LvDomain.Entities.Projects;

public class ProjectEndDateHistory : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateTime PreviousDate { get; set; }
    public DateTime NewDate { get; set; }
    public string Reason { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ChangedAt { get; set; }
}
