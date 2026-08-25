using LvApplication.Services.Workers;
using LvDomain.Entities.Workers;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Workers;

public class WorkerRepository : IWorkerRepository
{
    private readonly AppDbContext _context;

    public WorkerRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Worker?> GetByIdAsync(int id) =>
        _context.Workers.FirstOrDefaultAsync(w => w.Id == id);

    public async Task<(List<Worker> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize
    )
    {
        var totalCount = await _context.Workers.CountAsync();

        var items = await _context
            .Workers.OrderBy(w => w.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Worker worker)
    {
        _context.Workers.Add(worker);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Worker worker)
    {
        _context.Workers.Update(worker);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Worker worker)
    {
        _context.Workers.Remove(worker);
        await _context.SaveChangesAsync();
    }
}
