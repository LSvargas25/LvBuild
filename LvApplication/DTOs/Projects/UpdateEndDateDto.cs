namespace LvApplication.DTOs.Projects;

public class UpdateEndDateDto
{
    public DateTime NewEndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}
