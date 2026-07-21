namespace LvApplication.DTOs.Inventory;

public class CreateMaterialTicketDto
{
    public int SupplierId { get; set; }
    public int MaterialId { get; set; }
    public string? Description { get; set; }
    public string? InvoicePhotoPath { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }
}
