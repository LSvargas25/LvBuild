using LvApplication.Common;
using LvApplication.DTOs.Suppliers;

namespace LvApplication.Services.Suppliers;

public interface ISupplierService
{
    Task<SupplierResponseDto> CreateAsync(CreateSupplierDto request);
    Task<SupplierResponseDto> UpdateAsync(int id, UpdateSupplierDto request);
    Task<SupplierResponseDto> GetByIdAsync(int id);
    Task<PagedResult<SupplierResponseDto>> GetAllAsync(int pageNumber, int pageSize);
    Task DeleteAsync(int id);
}
