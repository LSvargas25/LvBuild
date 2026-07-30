using LvApplication.Common;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Services.Commercial;

public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductDto request, int createdByUserId, IEnumerable<string> actingUserRoles);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto request, IEnumerable<string> actingUserRoles);
    Task<ProductDto> ValidateAsync(int id, bool approve, int actingUserId, IEnumerable<string> actingUserRoles);
    Task<ProductDto> DeactivateAsync(int id);
    Task<ProductDto> ActivateAsync(int id);
    Task<ProductDto> GetByIdAsync(int id);
    Task<PagedResult<ProductDto>> GetAllAsync(int pageNumber, int pageSize, bool activeOnly);
}
