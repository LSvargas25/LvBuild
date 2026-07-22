namespace LvApplication.Services.Payroll;

public interface IPayrollRepository
{
    Task<LvDomain.Entities.Payroll.Payroll?> GetByIdAsync(int id);
    Task<bool> ExistsForSiteLogAsync(int siteLogId);
    Task<(List<LvDomain.Entities.Payroll.Payroll> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<(List<LvDomain.Entities.Payroll.Payroll> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize);
    Task<decimal> SumPaidTotalByChapterAsync(int projectId, int chapterId);
    Task AddAsync(LvDomain.Entities.Payroll.Payroll payroll);
    Task UpdateAsync(LvDomain.Entities.Payroll.Payroll payroll);
    Task DeleteAsync(LvDomain.Entities.Payroll.Payroll payroll);
}
