using LvDomain.Entities.Commercial;

namespace LvApplication.Services.Commercial;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
    Task AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(Invoice invoice);
    Task<int> CountByBranchAsync(int branchId);
    Task<(List<Invoice> Items, int TotalCount)> GetPagedByBranchAsync(int branchId, int pageNumber, int pageSize);
}
