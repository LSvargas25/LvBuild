using LvApplication.Common;
using LvApplication.DTOs.Branches;

namespace LvApplication.Services.Branches;

public interface IBranchService
{
    Task<BranchResponseDto> CreateAsync(CreateBranchDto request);
    Task<BranchResponseDto> UpdateAsync(int id, UpdateBranchDto request);
    Task<BranchResponseDto> AssignOperationsDirectorAsync(
        int id,
        AssignOperationsDirectorDto request
    );
    Task<BranchResponseDto> ActivateAsync(int id);
    Task<BranchResponseDto> DeactivateAsync(int id);
    Task DeleteAsync(int id, bool isGeneralManager);
    Task<BranchResponseDto> GetByIdAsync(
        int id,
        int currentUserId,
        IEnumerable<string> currentUserRoles
    );
    Task<PagedResult<BranchResponseDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        int currentUserId,
        IEnumerable<string> currentUserRoles
    );

    /// <summary>Active branches as id + name, for selection lists in other modules.</summary>
    Task<List<BranchOptionDto>> GetOptionsAsync();
}
