using LvDomain.Entities.Projects;

namespace LvApplication.Services.Projects;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id);
    Task<Project?> GetByOfferIdAsync(int offerId);
    Task<(List<Project> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Project project);
    Task<List<ProjectEndDateHistory>> GetEndDateHistoryAsync(int projectId);
}
