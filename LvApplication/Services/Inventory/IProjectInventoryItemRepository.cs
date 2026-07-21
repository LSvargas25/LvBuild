using LvDomain.Entities.Inventory;

namespace LvApplication.Services.Inventory;

public interface IProjectInventoryItemRepository
{
    Task<ProjectInventoryItem?> GetByProjectAndMaterialAsync(int projectId, int materialId);
    Task<List<ProjectInventoryItem>> GetByProjectAsync(int projectId);
    Task AddAsync(ProjectInventoryItem item);
    Task UpdateAsync(ProjectInventoryItem item);
    Task<int> CountWithQuantityAsync(int projectId);
}
