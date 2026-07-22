using LvDomain.Entities.Inventory;

namespace LvApplication.Services.Inventory;

public interface IMaterialTicketRepository
{
    Task<MaterialTicket?> GetByIdAsync(int id);
    Task<(List<MaterialTicket> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize);
    Task<decimal> SumAppliedTotalByChapterAsync(int projectId, int chapterId);
    Task<List<MaterialTicket>> GetAppliedInRangeAsync(int projectId, DateTime from, DateTime to);
    Task AddAsync(MaterialTicket ticket);
    Task UpdateAsync(MaterialTicket ticket);
    Task DeleteAsync(MaterialTicket ticket);
}
