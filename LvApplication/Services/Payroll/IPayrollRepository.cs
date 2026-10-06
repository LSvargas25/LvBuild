namespace LvApplication.Services.Payroll;

public interface IPayrollRepository
{
    Task<LvDomain.Entities.Payroll.Payroll?> GetByIdAsync(int id);
    Task<bool> ExistsForSiteLogAsync(int siteLogId);
    Task<(List<LvDomain.Entities.Payroll.Payroll> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize
    );
    Task<(List<LvDomain.Entities.Payroll.Payroll> Items, int TotalCount)> GetPagedByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    );
    Task<decimal> SumPaidTotalByChapterAsync(int projectId, int chapterId);

    /// <summary>Total of the project's payrolls paid (PaidAt) on the given Costa Rica days.</summary>
    Task<decimal> SumPaidInRangeAsync(int projectId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Balance of unpaid payrolls at the end of a Costa Rica day: weeks closed on or before it
    /// that were not yet paid by then (still pending, or paid later).
    /// </summary>
    Task<decimal> SumUnpaidAtAsync(int projectId, DateTime asOfDate);
    Task AddAsync(LvDomain.Entities.Payroll.Payroll payroll);
    Task UpdateAsync(LvDomain.Entities.Payroll.Payroll payroll);
    Task DeleteAsync(LvDomain.Entities.Payroll.Payroll payroll);
}
