using LvApplication.Services.Commercial;
using LvDomain.Entities.Commercial;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Commercial;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id) =>
        await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

    // Case-insensitive like the previous SQL Server collation, so "abc-1" and "ABC-1" are
    // still treated as the same SKU by the duplicate check. Translates to lower() on PostgreSQL.
#pragma warning disable CA1304, CA1311, CA1862 // culture-aware/OrdinalIgnoreCase overloads are not translatable to SQL
    public async Task<Product?> GetBySkuAsync(string sku) =>
        await _context.Products.FirstOrDefaultAsync(p => p.Sku.ToLower() == sku.ToLower());
#pragma warning restore CA1304, CA1311, CA1862

    public async Task AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly
    )
    {
        var query = _context.Products.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(p => p.ActiveStatus);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
