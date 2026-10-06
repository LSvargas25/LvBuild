using LvApplication.Services.Branches;
using LvDomain.Entities.Branches;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Branches;

public class BranchRepository : IBranchRepository
{
    private readonly AppDbContext _context;

    public BranchRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Branch> BranchesWithIndicator => _context.Branches.Include(b => b.Indicator);

    public Task<Branch?> GetByIdAsync(int id) =>
        BranchesWithIndicator.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<(List<Branch> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? operationsDirectorId
    )
    {
        var query = BranchesWithIndicator.AsQueryable();

        if (operationsDirectorId.HasValue)
        {
            query = query.Where(b => b.OperationsDirectorId == operationsDirectorId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(b => b.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<List<Branch>> GetActiveAsync() =>
        _context
            .Branches.AsNoTracking()
            .Where(b => b.Status == BranchStatus.Active)
            .OrderBy(b => b.Name)
            .ToListAsync();

    public async Task AddAsync(Branch branch)
    {
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Branch branch)
    {
        _context.Branches.Update(branch);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Branch branch)
    {
        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();
    }
}
