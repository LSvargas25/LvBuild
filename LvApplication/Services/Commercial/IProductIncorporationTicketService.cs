using LvApplication.Common;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Services.Commercial;

public interface IProductIncorporationTicketService
{
    Task<ProductIncorporationTicketDto> CreateAsync(
        CreateProductIncorporationTicketDto request,
        int createdByUserId,
        IEnumerable<string> actingUserRoles
    );
    Task<ProductIncorporationTicketDto> ValidateAsync(
        int id,
        bool approve,
        int actingUserId,
        IEnumerable<string> actingUserRoles
    );
    Task DeleteAsync(int id);
    Task<ProductIncorporationTicketDto> GetByIdAsync(int id);
    Task<PagedResult<ProductIncorporationTicketDto>> GetAllByBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    );
}
