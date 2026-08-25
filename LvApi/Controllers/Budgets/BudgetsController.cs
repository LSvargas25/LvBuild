using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Budgets;
using LvApplication.Services.Budgets;
using LvDomain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Budgets;

[ApiController]
[Route("api/budgets")]
[Authorize]
public class BudgetsController : ApiControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost]
    public async Task<ActionResult<BudgetResponseDto>> Create(CreateBudgetDto request)
    {
        var result = await _budgetService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BudgetResponseDto>> Update(int id, UpdateBudgetDto request)
    {
        var result = await _budgetService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("{id:int}/submit-for-review")]
    public async Task<ActionResult<BudgetResponseDto>> SubmitForReview(int id, SubmitForReviewDto request)
    {
        var result = await _budgetService.SubmitForReviewAsync(id, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/approve-internal")]
    public async Task<ActionResult<BudgetResponseDto>> ApproveInternal(int id)
    {
        var result = await _budgetService.ApproveInternalAsync(id, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/request-correction")]
    public async Task<ActionResult<BudgetResponseDto>> RequestCorrection(int id, RequestCorrectionDto request)
    {
        var result = await _budgetService.RequestCorrectionAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/withdraw-from-commercial")]
    public async Task<ActionResult<BudgetResponseDto>> WithdrawFromCommercial(int id, RequestCorrectionDto request)
    {
        var result = await _budgetService.WithdrawFromCommercialAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/mark-client-approved")]
    public async Task<ActionResult<BudgetResponseDto>> MarkClientApproved(int id)
    {
        var result = await _budgetService.MarkClientApprovedAsync(id, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<BudgetResponseDto>> Cancel(int id, CancelBudgetDto request)
    {
        var result = await _budgetService.CancelAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _budgetService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BudgetResponseDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BudgetStatus? status = null)
    {
        var result = await _budgetService.GetAllAsync(pageNumber, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BudgetResponseDto>> GetById(int id)
    {
        var result = await _budgetService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<BudgetHistoryResponseDto>>> GetHistory(int id)
    {
        var result = await _budgetService.GetHistoryAsync(id);
        return Ok(result);
    }
}
