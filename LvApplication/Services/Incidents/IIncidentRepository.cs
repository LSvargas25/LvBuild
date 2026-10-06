using LvDomain.Entities.Incidents;
using LvDomain.Enums;

namespace LvApplication.Services.Incidents;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(int id);
    Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<(List<Incident> Items, int TotalCount)> GetPagedByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    );
    Task<(int Count, decimal TotalCost)> GetApprovedSummaryByChapterAsync(
        int projectId,
        int chapterId
    );

    /// <summary>Total cost of the project's incidents in a status whose Date is in the range.</summary>
    Task<decimal> SumInRangeAsync(
        int projectId,
        IncidentStatus status,
        DateTime fromDate,
        DateTime toDate
    );
    Task AddAsync(Incident incident);
    Task UpdateAsync(Incident incident);
    Task DeleteAsync(Incident incident);
}
