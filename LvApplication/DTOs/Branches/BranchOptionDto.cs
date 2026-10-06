using LvDomain.Enums;

namespace LvApplication.DTOs.Branches;

/// <summary>Minimal branch data for selection lists (any authenticated user).</summary>
public class BranchOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BranchType BranchType { get; set; }
}
