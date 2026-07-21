namespace LvApplication.DTOs.Projects;

public class ProjectWorkerDto
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateTime AssignedAt { get; set; }
    public int AssignedByUserId { get; set; }
    public bool IsActive { get; set; }
}
