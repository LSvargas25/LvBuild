using LvApplication.Common;
using LvApplication.DTOs.Incidents;

namespace LvApplication.Services.Incidents;

public interface IIncidentService
{
    Task<IncidentDto> CreateAsync(CreateIncidentDto request, int createdByUserId);
    Task<IncidentDto> UpdateAsync(int id, UpdateIncidentDto request);
    Task<IncidentDto> ApproveAsync(int id, int approvedByUserId);
    Task DeleteAsync(int id);
    Task<IncidentDto> GetByIdAsync(int id);
    Task<PagedResult<IncidentDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<PagedResult<IncidentDto>> GetAllByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    );
}
