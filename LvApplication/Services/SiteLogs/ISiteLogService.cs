using LvApplication.Common;
using LvApplication.DTOs.SiteLogs;

namespace LvApplication.Services.SiteLogs;

public interface ISiteLogService
{
    Task<SiteLogDto> CreateAsync(CreateSiteLogDto request, int createdByUserId);
    Task<SiteLogDto> UpdateAsync(int id, UpdateSiteLogDto request);
    Task<SiteLogDto> SubmitToReviewAsync(int id);
    Task<SiteLogDto> RevertToDraftAsync(int id, RevertToDraftDto request);
    Task<SiteLogDto> ApproveAsync(int id, int approvedByUserId);
    Task DeleteAsync(int id);
    Task<SiteLogDto> GetByIdAsync(int id);
    Task<PagedResult<SiteLogDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<PagedResult<SiteLogDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize);

    // Internal hook for Fase 9 (Payroll) — not exposed via any controller endpoint yet.
    Task UpdateTotalPayrollAsync(int siteLogId, decimal amount);
}
