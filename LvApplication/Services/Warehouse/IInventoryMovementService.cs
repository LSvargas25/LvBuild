using LvApplication.Common;
using LvApplication.DTOs.Warehouse;

namespace LvApplication.Services.Warehouse;

public interface IInventoryMovementService
{
    Task<InventoryMovementDto> CreateAsync(CreateInventoryMovementDto request, int sentByUserId);
    Task<InventoryMovementDto> ValidateAsync(
        int id,
        bool approve,
        int actingUserId,
        IEnumerable<string> actingUserRoles
    );
    Task DeleteAsync(int id, IEnumerable<string> actingUserRoles);
    Task<InventoryMovementDto> GetByIdAsync(int id);
    Task<PagedResult<InventoryMovementDto>> GetAllByOriginBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    );
}
