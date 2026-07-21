using LvDomain.Enums;

namespace LvApplication.DTOs.Incidents;

public class IncidentDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public decimal TotalCost { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public List<IncidentMaterialResponseDto> Materials { get; set; } = new();
    public List<IncidentWorkerResponseDto> Workers { get; set; } = new();
}
