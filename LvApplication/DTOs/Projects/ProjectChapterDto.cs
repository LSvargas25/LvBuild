namespace LvApplication.DTOs.Projects;

public class ProjectChapterDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int ChapterId { get; set; }
    public decimal AssignedSoldTotal { get; set; }
    public decimal ActualCostTotal { get; set; }
    public decimal ChapterProfit { get; set; }
    public int IncidentCount { get; set; }
    public decimal? IncidentPercentage { get; set; }
}
