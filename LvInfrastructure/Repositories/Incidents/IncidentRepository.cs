using LvApplication.Services.Incidents;
using LvDomain.Entities.Incidents;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Incidents;

public class IncidentRepository : IIncidentRepository
{
    private readonly AppDbContext _context;

    public IncidentRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Incident> IncidentsWithChildren => _context.Incidents
        .Include(i => i.Materials)
        .Include(i => i.Workers);

    public Task<Incident?> GetByIdAsync(int id) =>
        IncidentsWithChildren.FirstOrDefaultAsync(i => i.Id == id);

    public async Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Incidents.CountAsync();

        var items = await IncidentsWithChildren
            .OrderBy(i => i.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Incident> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.Incidents.Where(i => i.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await IncidentsWithChildren
            .Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Incident incident)
    {
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Incident incident)
    {
        _context.Incidents.Update(incident);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Incident incident)
    {
        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();
    }
}
