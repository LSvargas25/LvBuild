using LvApplication.DTOs.Finance;
using LvApplication.Services.Finance;
using LvDomain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Finance;

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

    [HttpGet("projects/{projectId:int}/finance")]
    public async Task<ActionResult<ProjectFinanceDto>> GetFinance(int projectId, [FromQuery] FinancePeriod period, [FromQuery] DateTime date)
    {
        var result = await _financeService.GetFinanceAsync(projectId, period, date);
        return Ok(result);
    }
}
