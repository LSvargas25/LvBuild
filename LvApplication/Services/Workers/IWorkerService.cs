using LvApplication.Common;
using LvApplication.DTOs.Workers;

namespace LvApplication.Services.Workers;

public interface IWorkerService
{
    Task<WorkerResponseDto> CreateAsync(CreateWorkerDto request);
    Task<WorkerResponseDto> UpdateAsync(int id, UpdateWorkerDto request);
    Task<WorkerResponseDto> GetByIdAsync(int id);
    Task<PagedResult<WorkerResponseDto>> GetAllAsync(int pageNumber, int pageSize);
    Task DeleteAsync(int id);
}
