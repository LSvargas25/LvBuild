using LvDomain.Enums;

namespace LvApplication.DTOs.Warehouse;

public class InventoryMovementDto
{
    public int Id { get; set; }
    public int OriginBranchId { get; set; }
    public int? DestinationBranchId { get; set; }
    public int? DestinationProjectId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public InventoryMovementStatus Status { get; set; }
    public int SentByUserId { get; set; }
    public DateTime SentDate { get; set; }
    public int? ValidatedByUserId { get; set; }
    public DateTime? ValidatedDate { get; set; }
}
