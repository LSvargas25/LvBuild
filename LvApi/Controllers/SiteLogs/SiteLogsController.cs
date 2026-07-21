using System.Security.Claims;
using LvApplication.Common;
using LvApplication.DTOs.SiteLogs;
using LvApplication.Services.SiteLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.SiteLogs;

[ApiController]
[Route("api")]
[Authorize]
public class SiteLogsController : ControllerBase
{
    private readonly ISiteLogService _siteLogService;

    public SiteLogsController(ISiteLogService siteLogService)
    {
        _siteLogService = siteLogService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("site-logs")]
    public async Task<ActionResult<SiteLogDto>> Create(CreateSiteLogDto request)
    {
        var result = await _siteLogService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("site-logs/{id:int}")]
    public async Task<ActionResult<SiteLogDto>> Update(int id, UpdateSiteLogDto request)
    {
        var result = await _siteLogService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("site-logs/{id:int}/submit-review")]
    public async Task<ActionResult<SiteLogDto>> SubmitToReview(int id)
    {
        var result = await _siteLogService.SubmitToReviewAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("site-logs/{id:int}/revert-to-draft")]
    public async Task<ActionResult<SiteLogDto>> RevertToDraft(int id, RevertToDraftDto request)
    {
        var result = await _siteLogService.RevertToDraftAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("site-logs/{id:int}/approve")]
    public async Task<ActionResult<SiteLogDto>> Approve(int id)
    {
        var result = await _siteLogService.ApproveAsync(id, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpDelete("site-logs/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _siteLogService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("site-logs")]
    public async Task<ActionResult<PagedResult<SiteLogDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _siteLogService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("site-logs/{id:int}")]
    public async Task<ActionResult<SiteLogDto>> GetById(int id)
    {
        var result = await _siteLogService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("projects/{projectId:int}/site-logs")]
    public async Task<ActionResult<PagedResult<SiteLogDto>>> GetAllByProject(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _siteLogService.GetAllByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return int.Parse(userIdClaim!.Value);
    }
}
