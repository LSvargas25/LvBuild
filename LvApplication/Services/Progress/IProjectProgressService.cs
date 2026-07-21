using LvApplication.Common;
using LvApplication.DTOs.Progress;

namespace LvApplication.Services.Progress;

public interface IProjectProgressService
{
    Task<ProjectProgressDto> CalculateAndRecordAsync(int siteLogId);
    Task<PagedResult<ProjectProgressDto>> GetHistoryByProjectAsync(int projectId, int pageNumber, int pageSize);
}
