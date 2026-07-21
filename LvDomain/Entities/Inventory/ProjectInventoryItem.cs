using LvDomain.Common;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Projects;

namespace LvDomain.Entities.Inventory;

public class ProjectInventoryItem : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int MaterialId { get; set; }
    public MaterialCatalog Material { get; set; } = null!;

    public decimal CurrentQuantity { get; set; }
    public decimal ReferenceUnitCost { get; set; }
}
