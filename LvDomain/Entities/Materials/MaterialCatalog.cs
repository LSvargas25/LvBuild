using LvDomain.Common;

namespace LvDomain.Entities.Materials;

public class MaterialCatalog : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? UnitOfMeasure { get; set; }
}
