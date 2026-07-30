using LvApplication.Services.Commercial;
using LvDomain.Entities.Commercial;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Commercial;

public class ProductIncorporationTicketRepository : IProductIncorporationTicketRepository
{
    private readonly AppDbContext _context;

    public ProductIncorporationTicketRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductIncorporationTicket?> GetByIdAsync(int id) =>
        await _context.ProductIncorporationTickets.FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(ProductIncorporationTicket ticket)
    {
        _context.ProductIncorporationTickets.Add(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductIncorporationTicket ticket)
    {
        _context.ProductIncorporationTickets.Update(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ProductIncorporationTicket ticket)
    {
        _context.ProductIncorporationTickets.Remove(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<ProductIncorporationTicket> Items, int TotalCount)> GetPagedByBranchAsync(int branchId, int pageNumber, int pageSize)
    {
        var query = _context.ProductIncorporationTickets.Where(t => t.BranchId == branchId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
