using LvDomain.Entities.Inventory;
using LvDomain.Enums;

namespace LvApplication.Services.Inventory;

public interface IMaterialTicketRepository
{
    Task<MaterialTicket?> GetByIdAsync(int id);
    Task<(List<MaterialTicket> Items, int TotalCount)> GetPagedByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    );
    Task<decimal> SumAppliedTotalByChapterAsync(int projectId, int chapterId);
    Task<List<MaterialTicket>> GetAppliedInRangeAsync(
        int projectId,
        DateTime fromDate,
        DateTime toDate
    );

    /// <summary>Total of the project's tickets in a status created on the given Costa Rica days.</summary>
    Task<decimal> SumInRangeAsync(
        int projectId,
        MaterialTicketStatus status,
        DateTime fromDate,
        DateTime toDate
    );
    Task AddAsync(MaterialTicket ticket);
    Task UpdateAsync(MaterialTicket ticket);
    Task DeleteAsync(MaterialTicket ticket);
}
