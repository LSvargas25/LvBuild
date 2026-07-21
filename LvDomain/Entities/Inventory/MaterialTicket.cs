using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Projects;
using LvDomain.Entities.Suppliers;
using LvDomain.Enums;

namespace LvDomain.Entities.Inventory;

public class MaterialTicket : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int MaterialId { get; set; }
    public MaterialCatalog Material { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public string? Description { get; set; }
    public string? InvoicePhotoPath { get; set; }

    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    public MaterialTicketStatus Status { get; set; }
}
