using LvDomain.Entities.Progress;

namespace LvApplication.Services.Progress;

public interface IProjectProgressRepository
{
    Task AddAsync(ProjectProgress progress);
    Task<(List<ProjectProgress> Items, int TotalCount)> GetPagedByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    );
}
