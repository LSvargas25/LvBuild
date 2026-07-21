using LvApplication.Common;
using LvApplication.DTOs.Projects;

namespace LvApplication.Services.Projects;

public interface IProjectService
{
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto request, int createdByUserId);
    Task<ProjectDto> UpdateEndDateAsync(int id, UpdateEndDateDto request, int actingUserId);
    Task<ProjectDto> AssignWorkerAsync(int id, AssignWorkerDto request, int actingUserId);
    Task<ProjectDto> UnassignWorkerAsync(int id, int workerId);
    Task DeleteAsync(int id);
    Task<ProjectDto> GetByIdAsync(int id);
    Task<PagedResult<ProjectDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<List<ProjectEndDateHistoryDto>> GetEndDateHistoryAsync(int projectId);

    // Internal helpers for Fase 8/9 (SiteLog approval + Payroll payment). Not wired to any controller yet.
    Task IncrementWeekCounterAsync(int projectId);
    Task DecrementWeekCounterAsync(int projectId, IEnumerable<string> actingUserRoles);
}
