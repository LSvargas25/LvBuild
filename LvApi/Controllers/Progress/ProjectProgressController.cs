using LvApplication.Common;
using LvApplication.DTOs.Progress;
using LvApplication.Services.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Progress;

/// <summary>Project progress snapshots.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class ProjectProgressController : ControllerBase
{
    private readonly IProjectProgressService _projectProgressService;

    public ProjectProgressController(IProjectProgressService projectProgressService)
    {
        _projectProgressService = projectProgressService;
    }

    [HttpGet("projects/{projectId:int}/progress-history")]
    public async Task<ActionResult<PagedResult<ProjectProgressDto>>> GetHistory(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _projectProgressService.GetHistoryByProjectAsync(
            projectId,
            pageNumber,
            pageSize
        );
        return Ok(result);
    }
}
