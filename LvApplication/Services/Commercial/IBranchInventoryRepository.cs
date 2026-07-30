using LvDomain.Entities.Commercial;

namespace LvApplication.Services.Commercial;

public interface IBranchInventoryRepository
{
    Task<BranchInventory?> GetByBranchAndProductAsync(int branchId, int productId);
    Task AddAsync(BranchInventory item);
    Task UpdateAsync(BranchInventory item);
    Task<List<BranchInventory>> GetByBranchAsync(int branchId);
}
