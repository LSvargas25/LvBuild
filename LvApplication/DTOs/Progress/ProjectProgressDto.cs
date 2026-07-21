namespace LvApplication.DTOs.Progress;

public class ProjectProgressDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SiteLogId { get; set; }
    public decimal ProgressPercentage { get; set; }
    public DateTime CalculatedAt { get; set; }
}
