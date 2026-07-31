using LvDomain.Enums;

namespace LvApplication.DTOs.Workers;

public class UpdateWorkerDto
{
    public string Name { get; set; } = string.Empty;
    public string? PersonalId { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? Birthday { get; set; }
    public ActiveStatus Status { get; set; }
    public WorkerCategory Category { get; set; }
    public WorkerType Type { get; set; }
    public decimal HourlyRate { get; set; }
    public int? BranchId { get; set; }
}
