using LvApplication.Services.Payroll;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Payroll;

public class PayrollRepository : IPayrollRepository
{
    private readonly AppDbContext _context;

    public PayrollRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<LvDomain.Entities.Payroll.Payroll> PayrollsWithDetails => _context.Payrolls
        .Include(p => p.Details).ThenInclude(d => d.Payments);

    public Task<LvDomain.Entities.Payroll.Payroll?> GetByIdAsync(int id) =>
        PayrollsWithDetails.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExistsForSiteLogAsync(int siteLogId) =>
        _context.Payrolls.AnyAsync(p => p.SiteLogId == siteLogId);

    public async Task<(List<LvDomain.Entities.Payroll.Payroll> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Payrolls.CountAsync();

        var items = await PayrollsWithDetails
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<LvDomain.Entities.Payroll.Payroll> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.Payrolls.Where(p => p.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await PayrollsWithDetails
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<decimal> SumPaidTotalByChapterAsync(int projectId, int chapterId) =>
        await _context.Payrolls
            .Where(p => p.ProjectId == projectId && p.ChapterId == chapterId && p.Status == PayrollStatus.Paid)
            .SumAsync(p => (decimal?)p.TotalPayroll) ?? 0m;

    public async Task AddAsync(LvDomain.Entities.Payroll.Payroll payroll)
    {
        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LvDomain.Entities.Payroll.Payroll payroll)
    {
        _context.Payrolls.Update(payroll);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(LvDomain.Entities.Payroll.Payroll payroll)
    {
        _context.Payrolls.Remove(payroll);
        await _context.SaveChangesAsync();
    }
}
