using LvDomain.Entities.Budgets;
using LvDomain.Enums;

namespace LvApplication.Services.Budgets;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(int id);
    Task<(List<Budget> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        BudgetStatus? status
    );
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(Budget budget);
    Task<List<BudgetHistory>> GetHistoryAsync(int budgetId);

    /// <summary>The offer made from the budget and the project started from it, if they exist.</summary>
    Task<(int? OfferId, int? ProjectId)> GetOfferAndProjectIdsAsync(int budgetId);
}
