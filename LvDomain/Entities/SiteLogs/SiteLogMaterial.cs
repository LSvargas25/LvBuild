using LvDomain.Common;
using LvDomain.Entities.Materials;

namespace LvDomain.Entities.SiteLogs;

public class SiteLogMaterial : BaseEntity
{
    public int SiteLogId { get; set; }
    public SiteLog SiteLog { get; set; } = null!;

    public int MaterialId { get; set; }
    public MaterialCatalog Material { get; set; } = null!;

    public decimal QuantityUsed { get; set; }
}
