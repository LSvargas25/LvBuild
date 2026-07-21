namespace LvApplication.DTOs.Projects;

public class ProjectEndDateHistoryDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime PreviousDate { get; set; }
    public DateTime NewDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ChangedAt { get; set; }
}
