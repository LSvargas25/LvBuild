using LvDomain.Entities.Commercial;

namespace LvApplication.Services.Commercial;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
    Task AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(Invoice invoice);

    /// <summary>Invoices of the branch that already received a consecutive number.</summary>
    Task<int> CountNumberedByBranchAsync(int branchId);
    Task<(List<Invoice> Items, int TotalCount)> GetPagedByBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    );
}
