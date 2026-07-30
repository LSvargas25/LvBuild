using LvDomain.Common;
using LvDomain.Entities.Branches;

namespace LvDomain.Entities.Commercial;

public class BranchInventory : BaseEntity
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal MinimumStock { get; set; }
}
