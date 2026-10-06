using LvApplication.DTOs.Finance;
using LvApplication.Services.Finance;
using LvDomain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Finance;

/// <summary>Project finance: budget vs actual.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class ProjectFinanceController : ControllerBase
{
    private readonly IProjectFinanceService _financeService;

    public ProjectFinanceController(IProjectFinanceService financeService)
    {
        _financeService = financeService;
    }

    /// <summary>Returns the project's direct and pending expenses, applied material tickets and hours worked for the week, month or year that contains the given date (Costa Rica calendar).</summary>
    [HttpGet("projects/{projectId:int}/finance")]
    public async Task<ActionResult<ProjectFinanceDto>> GetFinance(
        int projectId,
        [FromQuery] FinancePeriod period,
        [FromQuery] DateTime date
    )
    {
        var result = await _financeService.GetFinanceAsync(projectId, period, date);
        return Ok(result);
    }
}
