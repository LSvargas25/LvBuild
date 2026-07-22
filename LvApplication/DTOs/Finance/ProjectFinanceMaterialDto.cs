namespace LvApplication.DTOs.Finance;

public class ProjectFinanceMaterialDto
{
    public string MaterialName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Total { get; set; }
    public DateTime Date { get; set; }
}
