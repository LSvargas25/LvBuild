using LvApplication.Services.Progress;
using LvDomain.Entities.Progress;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Progress;

public class ProjectProgressRepository : IProjectProgressRepository
{
    private readonly AppDbContext _context;

    public ProjectProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProjectProgress progress)
    {
        _context.ProjectProgresses.Add(progress);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<ProjectProgress> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.ProjectProgresses.Where(p => p.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.CalculatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
