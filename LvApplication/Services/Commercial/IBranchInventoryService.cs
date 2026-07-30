using LvApplication.DTOs.Commercial;

namespace LvApplication.Services.Commercial;

public interface IBranchInventoryService
{
    Task<List<BranchInventoryDto>> GetByBranchAsync(int branchId);
}
