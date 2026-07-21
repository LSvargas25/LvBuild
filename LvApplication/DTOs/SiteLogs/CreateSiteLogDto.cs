namespace LvApplication.DTOs.SiteLogs;

public class CreateSiteLogDto
{
    public int ProjectId { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string TaskDescription { get; set; } = string.Empty;
    public string? PendingTasks { get; set; }
    public List<SiteLogWorkerDto> Workers { get; set; } = new();
    public List<SiteLogMaterialDto> Materials { get; set; } = new();
    public List<SiteLogEquipmentDto> Equipment { get; set; } = new();
}
