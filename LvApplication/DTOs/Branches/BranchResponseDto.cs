using LvDomain.Enums;

namespace LvApplication.DTOs.Branches;

public class BranchResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public BranchStatus Status { get; set; }
    public BranchType BranchType { get; set; }
    public int OperationsDirectorId { get; set; }
    public int? BranchAdminId { get; set; }
    public int? BusinessManagerId { get; set; }
    public BranchIndicatorDto? Indicator { get; set; }
}
