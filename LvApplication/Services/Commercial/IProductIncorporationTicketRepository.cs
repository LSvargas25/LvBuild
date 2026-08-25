using LvDomain.Entities.Commercial;

namespace LvApplication.Services.Commercial;

public interface IProductIncorporationTicketRepository
{
    Task<ProductIncorporationTicket?> GetByIdAsync(int id);
    Task AddAsync(ProductIncorporationTicket ticket);
    Task UpdateAsync(ProductIncorporationTicket ticket);
    Task DeleteAsync(ProductIncorporationTicket ticket);
    Task<(List<ProductIncorporationTicket> Items, int TotalCount)> GetPagedByBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    );
}
