using LvDomain.Common;
using LvDomain.Enums;

namespace LvDomain.Entities.Suppliers;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ActiveStatus Status { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PersonalId { get; set; }
    public string? Email { get; set; }
}
