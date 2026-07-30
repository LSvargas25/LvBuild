namespace LvApplication.DTOs.Commercial;

public class CreateProductIncorporationTicketDto
{
    public int BranchId { get; set; }
    public int ProductId { get; set; }
    public int SupplierId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
