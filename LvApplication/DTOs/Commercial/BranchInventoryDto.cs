namespace LvApplication.DTOs.Commercial;

public class BranchInventoryDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MinimumStock { get; set; }
}
