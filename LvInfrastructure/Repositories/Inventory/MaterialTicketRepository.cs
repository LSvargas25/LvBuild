using LvApplication.Services.Inventory;
using LvDomain.Entities.Inventory;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Inventory;

public class MaterialTicketRepository : IMaterialTicketRepository
{
    private readonly AppDbContext _context;

    public MaterialTicketRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<MaterialTicket?> GetByIdAsync(int id) =>
        _context.MaterialTickets.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<(List<MaterialTicket> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.MaterialTickets.Where(t => t.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(t => t.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(MaterialTicket ticket)
    {
        _context.MaterialTickets.Add(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MaterialTicket ticket)
    {
        _context.MaterialTickets.Update(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(MaterialTicket ticket)
    {
        _context.MaterialTickets.Remove(ticket);
        await _context.SaveChangesAsync();
    }
}
