using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Suppliers;
using LvDomain.Enums;

namespace LvDomain.Entities.Commercial;

public class ProductIncorporationTicket : BaseEntity
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedDate { get; set; }

    public ProductIncorporationTicketStatus Status { get; set; }

    public int? ValidatedByUserId { get; set; }
    public User? ValidatedByUser { get; set; }
    public DateTime? ValidatedDate { get; set; }
}
