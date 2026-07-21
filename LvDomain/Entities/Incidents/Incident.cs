using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Projects;
using LvDomain.Enums;

namespace LvDomain.Entities.Incidents;

public class Incident : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public decimal TotalCost { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public ICollection<IncidentMaterial> Materials { get; set; } = new List<IncidentMaterial>();
    public ICollection<IncidentWorker> Workers { get; set; } = new List<IncidentWorker>();
}
