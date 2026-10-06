using LvApplication.Services.Commercial;
using LvDomain.Entities.Commercial;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Commercial;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(int id) =>
        await _context
            .Invoices.Include(i => i.Details)
                .ThenInclude(d => d.Product)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task AddAsync(Invoice invoice)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Invoice invoice)
    {
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountNumberedByBranchAsync(int branchId) =>
        await _context.Invoices.CountAsync(i => i.BranchId == branchId && i.InvoiceNumber != null);

    public async Task<(List<Invoice> Items, int TotalCount)> GetPagedByBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    )
    {
        var query = _context
            .Invoices.Include(i => i.Details)
                .ThenInclude(d => d.Product)
            .Include(i => i.Payments)
            .Where(i => i.BranchId == branchId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
