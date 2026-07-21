using LvDomain.Common;
using LvDomain.Entities.Workers;

namespace LvDomain.Entities.Incidents;

public class IncidentWorker : BaseEntity
{
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public decimal HoursUsed { get; set; }
}
