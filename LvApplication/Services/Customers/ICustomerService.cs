using LvApplication.Common;
using LvApplication.DTOs.Customers;

namespace LvApplication.Services.Customers;

public interface ICustomerService
{
    Task<CustomerResponseDto> CreateAsync(CreateCustomerDto request);
    Task<CustomerResponseDto> UpdateAsync(int id, UpdateCustomerDto request);
    Task<CustomerResponseDto> GetByIdAsync(int id);
    Task<PagedResult<CustomerResponseDto>> GetAllAsync(int pageNumber, int pageSize);
    Task DeleteAsync(int id);
}
