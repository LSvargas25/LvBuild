using LvApplication.Common;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Services.Payroll;

public interface IPayrollService
{
    Task<PayrollDto> CreateAsync(CreatePayrollDto request, int createdByUserId);
    Task<PayrollDto> UpdateAsync(int id, UpdatePayrollDto request);
    Task<PayrollDto> MarkAsPaidAsync(int id);
    Task DeleteAsync(int id);
    Task<PayrollDto> GetByIdAsync(int id);
    Task<PagedResult<PayrollDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<PagedResult<PayrollDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize);
}
