using LvApplication.Services.Materials;
using LvDomain.Entities.Materials;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Materials;

public class MaterialCatalogRepository : IMaterialCatalogRepository
{
    private readonly AppDbContext _context;

    public MaterialCatalogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<MaterialCatalog?> GetByIdAsync(int id) =>
        _context.MaterialCatalogs.FirstOrDefaultAsync(m => m.Id == id);
}
