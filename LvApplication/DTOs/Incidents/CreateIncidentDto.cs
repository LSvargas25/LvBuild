namespace LvApplication.DTOs.Incidents;

public class CreateIncidentDto
{
    public int ProjectId { get; set; }
    public int? ChapterId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<IncidentMaterialDto> Materials { get; set; } = new();
    public List<IncidentWorkerDto> Workers { get; set; } = new();
}
