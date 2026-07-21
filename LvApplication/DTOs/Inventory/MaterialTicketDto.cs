using LvDomain.Enums;

namespace LvApplication.DTOs.Inventory;

public class MaterialTicketDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SupplierId { get; set; }
    public int MaterialId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
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
