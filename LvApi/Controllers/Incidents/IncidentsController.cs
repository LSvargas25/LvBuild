using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Incidents;
using LvApplication.Services.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Incidents;

[ApiController]
[Route("api")]
[Authorize]
public class IncidentsController : ApiControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("incidents")]
    public async Task<ActionResult<IncidentDto>> Create(CreateIncidentDto request)
    {
        var result = await _incidentService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("incidents/{id:int}")]
    public async Task<ActionResult<IncidentDto>> Update(int id, UpdateIncidentDto request)
    {
        var result = await _incidentService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("incidents/{id:int}/approve")]
    public async Task<ActionResult<IncidentDto>> Approve(int id)
    {
        var result = await _incidentService.ApproveAsync(id, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpDelete("incidents/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _incidentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("incidents")]
    public async Task<ActionResult<PagedResult<IncidentDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _incidentService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("incidents/{id:int}")]
    public async Task<ActionResult<IncidentDto>> GetById(int id)
    {
        var result = await _incidentService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("projects/{projectId:int}/incidents")]
    public async Task<ActionResult<PagedResult<IncidentDto>>> GetAllByProject(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _incidentService.GetAllByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }
}
