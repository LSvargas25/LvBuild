using LvApplication.Services.Warehouse;
using LvDomain.Entities.Warehouse;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Warehouse;

public class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly AppDbContext _context;

    public InventoryMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryMovement?> GetByIdAsync(int id) =>
        await _context.InventoryMovements.FirstOrDefaultAsync(m => m.Id == id);

    public async Task AddAsync(InventoryMovement movement)
    {
        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(InventoryMovement movement)
    {
        _context.InventoryMovements.Update(movement);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(InventoryMovement movement)
    {
        _context.InventoryMovements.Remove(movement);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<InventoryMovement> Items, int TotalCount)> GetPagedByOriginBranchAsync(int branchId, int pageNumber, int pageSize)
    {
        var query = _context.InventoryMovements.Where(m => m.OriginBranchId == branchId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.SentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
