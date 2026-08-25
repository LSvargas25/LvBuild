using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Inventory;
using LvApplication.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Inventory;

[ApiController]
[Route("api")]
[Authorize]
public class MaterialTicketsController : ApiControllerBase
{
    private readonly IMaterialTicketService _materialTicketService;

    public MaterialTicketsController(IMaterialTicketService materialTicketService)
    {
        _materialTicketService = materialTicketService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("projects/{projectId:int}/material-tickets")]
    public async Task<ActionResult<MaterialTicketDto>> Create(
        int projectId,
        CreateMaterialTicketDto request
    )
    {
        var result = await _materialTicketService.CreateAsync(
            projectId,
            request,
            GetCurrentUserId()
        );
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("material-tickets/{id:int}")]
    public async Task<ActionResult<MaterialTicketDto>> Update(
        int id,
        UpdateMaterialTicketDto request
    )
    {
        var result = await _materialTicketService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("material-tickets/{id:int}/apply")]
    public async Task<ActionResult<MaterialTicketDto>> Apply(int id)
    {
        var result = await _materialTicketService.ApplyAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("material-tickets/{id:int}/archive")]
    public async Task<ActionResult<MaterialTicketDto>> Archive(int id)
    {
        var result = await _materialTicketService.ArchiveAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("material-tickets/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _materialTicketService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("projects/{projectId:int}/material-tickets")]
    public async Task<ActionResult<PagedResult<MaterialTicketDto>>> GetAllByProject(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _materialTicketService.GetAllByProjectAsync(
            projectId,
            pageNumber,
            pageSize
        );
        return Ok(result);
    }

    [HttpGet("material-tickets/{id:int}")]
    public async Task<ActionResult<MaterialTicketDto>> GetById(int id)
    {
        var result = await _materialTicketService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("projects/{projectId:int}/inventory")]
    public async Task<ActionResult<List<ProjectInventoryItemDto>>> GetInventory(int projectId)
    {
        var result = await _materialTicketService.GetInventoryAsync(projectId);
        return Ok(result);
    }
}
