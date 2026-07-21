using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Workers;

namespace LvDomain.Entities.Projects;

public class ProjectWorker : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public int AssignedByUserId { get; set; }
    public User AssignedByUser { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
