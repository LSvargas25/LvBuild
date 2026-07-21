using LvDomain.Common;
using LvDomain.Enums;

namespace LvDomain.Entities.SiteLogs;

public class SiteLogEquipment : BaseEntity
{
    public int SiteLogId { get; set; }
    public SiteLog SiteLog { get; set; } = null!;

    public EquipmentType EquipmentType { get; set; }
    public string Description { get; set; } = string.Empty;
}
