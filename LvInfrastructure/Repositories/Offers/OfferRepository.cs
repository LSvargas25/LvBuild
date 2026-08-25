using LvApplication.Services.Offers;
using LvDomain.Entities.Offers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Offers;

public class OfferRepository : IOfferRepository
{
    private readonly AppDbContext _context;

    public OfferRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Offer> OffersWithChapters => _context.Offers.Include(o => o.Chapters);

    public Task<Offer?> GetByIdAsync(int id) =>
        OffersWithChapters.FirstOrDefaultAsync(o => o.Id == id);

    public Task<Offer?> GetByBudgetIdAsync(int budgetId) =>
        OffersWithChapters.FirstOrDefaultAsync(o => o.BudgetId == budgetId);

    public async Task<(List<Offer> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        OfferStatus? status
    )
    {
        var query = OffersWithChapters.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(o => o.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<int> CountByOfferNumberPrefixAsync(string prefix) =>
        _context.Offers.CountAsync(o => o.OfferNumber.StartsWith(prefix));

    public async Task AddAsync(Offer offer)
    {
        _context.Offers.Add(offer);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Offer offer)
    {
        _context.Offers.Update(offer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Offer offer)
    {
        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync();
    }
}
