using LvApplication.Services.Projects;
using LvDomain.Entities.Projects;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Projects;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Project> ProjectsWithWorkers => _context.Projects.Include(p => p.Workers);

    public Task<Project?> GetByIdAsync(int id) =>
        ProjectsWithWorkers.FirstOrDefaultAsync(p => p.Id == id);

    public Task<Project?> GetByOfferIdAsync(int offerId) =>
        ProjectsWithWorkers.FirstOrDefaultAsync(p => p.OfferId == offerId);

    public async Task<(List<Project> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize
    )
    {
        var totalCount = await _context.Projects.CountAsync();

        var items = await ProjectsWithWorkers
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Project project)
    {
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    public Task<List<ProjectEndDateHistory>> GetEndDateHistoryAsync(int projectId) =>
        _context
            .ProjectEndDateHistories.Where(h => h.ProjectId == projectId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

    public Task<Dictionary<int, string>> GetNamesAsync(IReadOnlyCollection<int> projectIds) =>
        _context
            .Projects.Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Budget.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name);
}
