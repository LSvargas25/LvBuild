using LvDomain.Common;

namespace LvDomain.Entities.Branches;

public class BranchIndicator : BaseEntity
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    // TODO: recalcular desde Project/Commercial/Warehouse en fases posteriores
    public decimal Profit { get; set; } = 0;
    public decimal Losses { get; set; } = 0;
    public decimal DirectExpenses { get; set; } = 0;
    public decimal IndirectExpenses { get; set; } = 0;
    public int TotalWorkers { get; set; }
    public int TotalMaterials { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
