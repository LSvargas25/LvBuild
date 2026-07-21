using LvDomain.Common;
using LvDomain.Entities.Materials;

namespace LvDomain.Entities.Incidents;

public class IncidentMaterial : BaseEntity
{
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public int MaterialId { get; set; }
    public MaterialCatalog Material { get; set; } = null!;

    public decimal Quantity { get; set; }
}
