using LvApplication.DTOs.Projects;
using LvApplication.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Projects;

/// <summary>Project chapters: sold vs actual cost per chapter.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class ProjectChaptersController : ControllerBase
{
    private readonly IProjectChapterService _projectChapterService;

    public ProjectChaptersController(IProjectChapterService projectChapterService)
    {
        _projectChapterService = projectChapterService;
    }

    [HttpGet("projects/{projectId:int}/chapters")]
    public async Task<ActionResult<List<ProjectChapterDto>>> GetByProject(int projectId)
    {
        var result = await _projectChapterService.GetByProjectAsync(projectId);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,ProjectAdmin")]
    [HttpPut("projects/{projectId:int}/chapters/{chapterId:int}/assigned-sold-total")]
    public async Task<ActionResult<ProjectChapterDto>> UpdateAssignedSoldTotal(
        int projectId,
        int chapterId,
        UpdateAssignedSoldTotalDto request
    )
    {
        var result = await _projectChapterService.UpdateAssignedSoldTotalAsync(
            projectId,
            chapterId,
            request.AssignedSoldTotal
        );
        return Ok(result);
    }
}
