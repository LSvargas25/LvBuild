using LvDomain.Enums;

namespace LvApplication.DTOs.SiteLogs;

public class SiteLogEquipmentResponseDto
{
    public int Id { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public string Description { get; set; } = string.Empty;
}
