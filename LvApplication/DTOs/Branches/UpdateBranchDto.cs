using LvDomain.Enums;

namespace LvApplication.DTOs.Branches;

public class UpdateBranchDto
{
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public BranchType BranchType { get; set; }
    public int? BranchAdminId { get; set; }
    public int? BusinessManagerId { get; set; }
}
