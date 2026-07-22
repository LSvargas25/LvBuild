using LvApplication.Services.SiteLogs;
using LvDomain.Entities.SiteLogs;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.SiteLogs;

public class SiteLogRepository : ISiteLogRepository
{
    private readonly AppDbContext _context;

    public SiteLogRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<SiteLog> SiteLogsWithChildren => _context.SiteLogs
        .Include(s => s.Workers)
        .Include(s => s.Materials)
        .Include(s => s.Equipment);

    public Task<SiteLog?> GetByIdAsync(int id) =>
        SiteLogsWithChildren.FirstOrDefaultAsync(s => s.Id == id);

    public Task<bool> ExistsForProjectAndWeekAsync(int projectId, DateTime weekStart) =>
        _context.SiteLogs.AnyAsync(s => s.ProjectId == projectId && s.WeekStart == weekStart);

    public async Task<(List<SiteLog> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.SiteLogs.CountAsync();

        var items = await SiteLogsWithChildren
            .OrderBy(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<SiteLog> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.SiteLogs.Where(s => s.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await SiteLogsWithChildren
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<List<SiteLog>> GetInRangeAsync(int projectId, DateTime from, DateTime to) =>
        SiteLogsWithChildren
            .Where(s => s.ProjectId == projectId && s.WeekStart >= from && s.WeekEnd <= to)
            .OrderBy(s => s.WeekStart)
            .ToListAsync();

    public async Task AddAsync(SiteLog siteLog)
    {
        _context.SiteLogs.Add(siteLog);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SiteLog siteLog)
    {
        _context.SiteLogs.Update(siteLog);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(SiteLog siteLog)
    {
        _context.SiteLogs.Remove(siteLog);
        await _context.SaveChangesAsync();
    }
}
