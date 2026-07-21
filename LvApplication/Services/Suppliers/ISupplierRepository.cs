using LvDomain.Entities.Suppliers;

namespace LvApplication.Services.Suppliers;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(int id);
    Task<(List<Supplier> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
}
