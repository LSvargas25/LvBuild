using LvApplication.Common;
using LvApplication.DTOs.Budgets;
using LvDomain.Enums;

namespace LvApplication.Services.Budgets;

public interface IBudgetService
{
    Task<BudgetResponseDto> CreateAsync(CreateBudgetDto request, int createdByUserId);
    Task<BudgetResponseDto> UpdateAsync(int id, UpdateBudgetDto request);
    Task<BudgetResponseDto> SubmitForReviewAsync(int id, int actingUserId);
    Task<BudgetResponseDto> ApproveInternalAsync(int id, int actingUserId);
    Task<BudgetResponseDto> RequestCorrectionAsync(
        int id,
        RequestCorrectionDto request,
        int actingUserId
    );
    Task<BudgetResponseDto> WithdrawFromCommercialAsync(
        int id,
        RequestCorrectionDto request,
        int actingUserId
    );
    Task<BudgetResponseDto> MarkClientApprovedAsync(int id, int actingUserId);
    Task<BudgetResponseDto> CancelAsync(int id, CancelBudgetDto request, int actingUserId);
    Task DeleteAsync(int id);
    Task<BudgetResponseDto> GetByIdAsync(int id);
    Task<PagedResult<BudgetResponseDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        BudgetStatus? status
    );
    Task<List<BudgetHistoryResponseDto>> GetHistoryAsync(int budgetId);
}
