using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Warehouse;
using LvApplication.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Warehouse;

/// <summary>Stock movements from a warehouse to a branch or a project, validated on arrival.</summary>
[ApiController]
[Route("api/inventory-movements")]
[Authorize]
public class InventoryMovementsController : ApiControllerBase
{
    private readonly IInventoryMovementService _movementService;

    public InventoryMovementsController(IInventoryMovementService movementService)
    {
        _movementService = movementService;
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,BranchAdmin,BusinessManager")]
    [HttpPost]
    public async Task<ActionResult<InventoryMovementDto>> Create(CreateInventoryMovementDto request)
    {
        var result = await _movementService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/validate")]
    public async Task<ActionResult<InventoryMovementDto>> Validate(
        int id,
        ValidateInventoryMovementDto request
    )
    {
        var result = await _movementService.ValidateAsync(
            id,
            request.Approve,
            GetCurrentUserId(),
            GetCurrentRoles()
        );
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _movementService.DeleteAsync(id, GetCurrentRoles());
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryMovementDto>> GetById(int id)
    {
        var result = await _movementService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("~/api/branches/{branchId:int}/inventory-movements")]
    public async Task<ActionResult<PagedResult<InventoryMovementDto>>> GetAllByOriginBranch(
        int branchId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _movementService.GetAllByOriginBranchAsync(
            branchId,
            pageNumber,
            pageSize
        );
        return Ok(result);
    }
}
