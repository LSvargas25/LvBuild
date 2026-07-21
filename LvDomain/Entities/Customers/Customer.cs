using LvDomain.Common;
using LvDomain.Enums;

namespace LvDomain.Entities.Customers;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; }
    public ActiveStatus Status { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PersonalId { get; set; }
    public string? Email { get; set; }

    // TODO Fase 3: convertir en FK real hacia Branch cuando exista
    public int? BranchId { get; set; }
}
