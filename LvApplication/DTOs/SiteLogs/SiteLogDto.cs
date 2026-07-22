using LvDomain.Enums;

namespace LvApplication.DTOs.SiteLogs;

public class SiteLogDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? ChapterId { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string TaskDescription { get; set; } = string.Empty;
    public string? PendingTasks { get; set; }
    public decimal TotalPayroll { get; set; }
    public decimal TotalMaterials { get; set; }
    public decimal? ProgressPercentage { get; set; }
    public SiteLogStatus Status { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public List<SiteLogWorkerResponseDto> Workers { get; set; } = new();
    public List<SiteLogMaterialResponseDto> Materials { get; set; } = new();
    public List<SiteLogEquipmentResponseDto> Equipment { get; set; } = new();
}
