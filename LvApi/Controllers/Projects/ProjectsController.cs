using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Projects;
using LvApplication.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Projects;

/// <summary>Projects created from accepted offers: dates, assigned workers and running costs.</summary>
[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>Creates a project from a ClientAccepted offer.</summary>
    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectDto request)
    {
        var result = await _projectService.CreateProjectAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Lists projects (paged).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _projectService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>Returns a project with its counters and assigned workers.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id)
    {
        var result = await _projectService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Moves the estimated end date; the change and its reason are kept in the history.</summary>
    [Authorize(Roles = "GeneralManager,OperationsDirector,ProjectAdmin")]
    [HttpPut("{id:int}/end-date")]
    public async Task<ActionResult<ProjectDto>> UpdateEndDate(int id, UpdateEndDateDto request)
    {
        var result = await _projectService.UpdateEndDateAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>Assigns a worker to the project.</summary>
    [Authorize(Roles = "GeneralManager,OperationsDirector,ProjectAdmin")]
    [HttpPost("{id:int}/workers")]
    public async Task<ActionResult<ProjectDto>> AssignWorker(int id, AssignWorkerDto request)
    {
        var result = await _projectService.AssignWorkerAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>Removes a worker from the project.</summary>
    [Authorize(Roles = "GeneralManager,OperationsDirector,ProjectAdmin")]
    [HttpDelete("{id:int}/workers/{workerId:int}")]
    public async Task<ActionResult<ProjectDto>> UnassignWorker(int id, int workerId)
    {
        var result = await _projectService.UnassignWorkerAsync(id, workerId);
        return Ok(result);
    }

    /// <summary>Deletes a project.</summary>
    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _projectService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Returns the end-date change history.</summary>
    [HttpGet("{id:int}/end-date-history")]
    public async Task<ActionResult<List<ProjectEndDateHistoryDto>>> GetEndDateHistory(int id)
    {
        var result = await _projectService.GetEndDateHistoryAsync(id);
        return Ok(result);
    }
}
