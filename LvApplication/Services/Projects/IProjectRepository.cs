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

    /// <summary>Project id -> display name (the name of the budget the project was sold from).</summary>
    Task<Dictionary<int, string>> GetNamesAsync(IReadOnlyCollection<int> projectIds);
}
