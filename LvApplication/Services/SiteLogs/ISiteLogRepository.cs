using LvDomain.Entities.SiteLogs;

namespace LvApplication.Services.SiteLogs;

public interface ISiteLogRepository
{
    Task<SiteLog?> GetByIdAsync(int id);
    Task<bool> ExistsForProjectAndWeekAsync(int projectId, DateTime weekStart);
    Task<(List<SiteLog> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<(List<SiteLog> Items, int TotalCount)> GetPagedByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    );
    Task<List<SiteLog>> GetInRangeAsync(int projectId, DateTime fromDate, DateTime toDate);
    Task AddAsync(SiteLog siteLog);
    Task UpdateAsync(SiteLog siteLog);
    Task DeleteAsync(SiteLog siteLog);
}
