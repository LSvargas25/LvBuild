using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Enums;

namespace LvDomain.Entities.Commercial;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public string? Category { get; set; }

    public ProductStatus Status { get; set; }
    public bool ActiveStatus { get; set; } = true;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? ValidatedByUserId { get; set; }
    public User? ValidatedByUser { get; set; }
    public DateTime? ValidatedDate { get; set; }
}
