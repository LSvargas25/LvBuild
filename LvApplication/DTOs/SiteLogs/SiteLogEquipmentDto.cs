using LvDomain.Enums;

namespace LvApplication.DTOs.SiteLogs;

public class SiteLogEquipmentDto
{
    public EquipmentType EquipmentType { get; set; }
    public string Description { get; set; } = string.Empty;
}
