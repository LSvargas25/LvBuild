using LvApplication.Services.Inventory;
using LvDomain.Entities.Inventory;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Inventory;

public class ProjectInventoryItemRepository : IProjectInventoryItemRepository
{
    private readonly AppDbContext _context;

    public ProjectInventoryItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ProjectInventoryItem?> GetByProjectAndMaterialAsync(
        int projectId,
        int materialId
    ) =>
        _context.ProjectInventoryItems.FirstOrDefaultAsync(i =>
            i.ProjectId == projectId && i.MaterialId == materialId
        );

    public Task<ProjectInventoryItem?> GetByProjectAndProductAsync(int projectId, int productId) =>
        _context.ProjectInventoryItems.FirstOrDefaultAsync(i =>
            i.ProjectId == projectId && i.ProductId == productId
        );

    public Task<List<ProjectInventoryItem>> GetByProjectAsync(int projectId) =>
        _context
            .ProjectInventoryItems.Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.Id)
            .ToListAsync();

    public async Task AddAsync(ProjectInventoryItem item)
    {
        _context.ProjectInventoryItems.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjectInventoryItem item)
    {
        _context.ProjectInventoryItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public Task<int> CountWithQuantityAsync(int projectId) =>
        _context.ProjectInventoryItems.CountAsync(i =>
            i.ProjectId == projectId && i.CurrentQuantity > 0
        );
}
