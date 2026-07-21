using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Enums;

namespace LvDomain.Entities.Branches;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public BranchStatus Status { get; set; }
    public BranchType BranchType { get; set; }

    public int OperationsDirectorId { get; set; }
    public User OperationsDirector { get; set; } = null!;

    public int? BranchAdminId { get; set; }
    public User? BranchAdmin { get; set; }

    public int? BusinessManagerId { get; set; }
    public User? BusinessManager { get; set; }

    public BranchIndicator? Indicator { get; set; }
}
