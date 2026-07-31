namespace LvApplication.DTOs.Warehouse;

public class CreateInventoryMovementDto
{
    public int OriginBranchId { get; set; }
    public int? DestinationBranchId { get; set; }
    public int? DestinationProjectId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
}
