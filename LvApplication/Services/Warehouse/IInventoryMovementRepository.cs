using LvDomain.Entities.Warehouse;

namespace LvApplication.Services.Warehouse;

public interface IInventoryMovementRepository
{
    Task<InventoryMovement?> GetByIdAsync(int id);
    Task AddAsync(InventoryMovement movement);
    Task UpdateAsync(InventoryMovement movement);
    Task DeleteAsync(InventoryMovement movement);
    Task<(List<InventoryMovement> Items, int TotalCount)> GetPagedByOriginBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    );
}
