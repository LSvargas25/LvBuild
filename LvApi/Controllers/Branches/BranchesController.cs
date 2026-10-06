using System.Globalization;
using System.Security.Claims;
using LvApplication.Common;
using LvApplication.DTOs.Branches;
using LvApplication.Services.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Branches;

/// <summary>Branches (offices, stores, warehouses) and their indicators.</summary>
[ApiController]
[Route("api/branches")]
[Authorize(Roles = "GeneralManager,OperationsDirector")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpPost]
    public async Task<ActionResult<BranchResponseDto>> Create(CreateBranchDto request)
    {
        var result = await _branchService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BranchResponseDto>> Update(int id, UpdateBranchDto request)
    {
        var result = await _branchService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager")]
    [HttpPost("{id:int}/assign-operations-director")]
    public async Task<ActionResult<BranchResponseDto>> AssignOperationsDirector(
        int id,
        AssignOperationsDirectorDto request
    )
    {
        var result = await _branchService.AssignOperationsDirectorAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<BranchResponseDto>> Activate(int id)
    {
        var result = await _branchService.ActivateAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<BranchResponseDto>> Deactivate(int id)
    {
        var result = await _branchService.DeactivateAsync(id);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var isGeneralManager = User.IsInRole("GeneralManager");
        await _branchService.DeleteAsync(id, isGeneralManager);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BranchResponseDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var (userId, roles) = GetCurrentUser();
        var result = await _branchService.GetAllAsync(pageNumber, pageSize, userId, roles);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BranchResponseDto>> GetById(int id)
    {
        var (userId, roles) = GetCurrentUser();
        var result = await _branchService.GetByIdAsync(id, userId, roles);
        return Ok(result);
    }

    private (int UserId, List<string> Roles) GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        var userId = int.Parse(userIdClaim!.Value, CultureInfo.InvariantCulture);
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        return (userId, roles);
    }
}
