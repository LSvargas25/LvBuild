using LvApplication.Services.Budgets;
using LvDomain.Entities.Budgets;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Budgets;

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _context;

    public BudgetRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Budget> BudgetsWithGraph => _context.Budgets
        .Include(b => b.Chapters).ThenInclude(c => c.Activities).ThenInclude(a => a.Materials)
        .Include(b => b.Chapters).ThenInclude(c => c.Activities).ThenInclude(a => a.Equipment)
        .Include(b => b.Chapters).ThenInclude(c => c.Activities).ThenInclude(a => a.Labor);

    public Task<Budget?> GetByIdAsync(int id) =>
        BudgetsWithGraph.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<(List<Budget> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, BudgetStatus? status)
    {
        var query = BudgetsWithGraph.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(b => b.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Budget budget)
    {
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Budget budget)
    {
        _context.Budgets.Update(budget);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Budget budget)
    {
        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
    }

    public async Task<List<BudgetHistory>> GetHistoryAsync(int budgetId) =>
        await _context.BudgetHistories
            .Where(h => h.BudgetId == budgetId)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();
}
