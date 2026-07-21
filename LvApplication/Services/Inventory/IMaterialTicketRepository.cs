using LvDomain.Entities.Inventory;

namespace LvApplication.Services.Inventory;

public interface IMaterialTicketRepository
{
    Task<MaterialTicket?> GetByIdAsync(int id);
    Task<(List<MaterialTicket> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize);
    Task AddAsync(MaterialTicket ticket);
    Task UpdateAsync(MaterialTicket ticket);
    Task DeleteAsync(MaterialTicket ticket);
}
