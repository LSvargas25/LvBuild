using LvApplication.Services.Commercial;
using LvDomain.Entities.Commercial;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Commercial;

public class BranchInventoryRepository : IBranchInventoryRepository
{
    private readonly AppDbContext _context;

    public BranchInventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BranchInventory?> GetByBranchAndProductAsync(int branchId, int productId) =>
        await _context.BranchInventories.FirstOrDefaultAsync(i => i.BranchId == branchId && i.ProductId == productId);

    public async Task AddAsync(BranchInventory item)
    {
        _context.BranchInventories.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BranchInventory item)
    {
        _context.BranchInventories.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task<List<BranchInventory>> GetByBranchAsync(int branchId) =>
        await _context.BranchInventories
            .Include(i => i.Product)
            .Where(i => i.BranchId == branchId)
            .OrderBy(i => i.Product.Name)
            .ToListAsync();
}
