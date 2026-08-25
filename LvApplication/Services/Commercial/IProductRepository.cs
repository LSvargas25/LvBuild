using LvDomain.Entities.Commercial;

namespace LvApplication.Services.Commercial;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetBySkuAsync(string sku);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly
    );
}
