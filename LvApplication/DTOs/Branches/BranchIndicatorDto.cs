namespace LvApplication.DTOs.Branches;

public class BranchIndicatorDto
{
    public decimal Profit { get; set; }
    public decimal Losses { get; set; }
    public decimal DirectExpenses { get; set; }
    public decimal IndirectExpenses { get; set; }
    public int TotalWorkers { get; set; }
    public int TotalMaterials { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
