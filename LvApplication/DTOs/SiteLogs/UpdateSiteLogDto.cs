namespace LvApplication.DTOs.SiteLogs;

public class UpdateSiteLogDto
{
    public string TaskDescription { get; set; } = string.Empty;
    public string? PendingTasks { get; set; }
    public List<SiteLogWorkerDto> Workers { get; set; } = new();
    public List<SiteLogMaterialDto> Materials { get; set; } = new();
    public List<SiteLogEquipmentDto> Equipment { get; set; } = new();
}
