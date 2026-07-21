using LvDomain.Entities.Materials;

namespace LvApplication.Services.Materials;

public interface IMaterialCatalogRepository
{
    Task<MaterialCatalog?> GetByIdAsync(int id);
}
