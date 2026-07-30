using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class ProductIncorporationTicketDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int ProductId { get; set; }
    public int SupplierId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public ProductIncorporationTicketStatus Status { get; set; }
    public int? ValidatedByUserId { get; set; }
    public DateTime? ValidatedDate { get; set; }
}
