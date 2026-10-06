using LvApplication.DTOs.Commercial;
using LvApplication.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Commercial;

/// <summary>Stock of products per branch.</summary>
[ApiController]
[Route("api/branches/{branchId:int}/inventory")]
[Authorize]
public class BranchInventoriesController : ControllerBase
{
    private readonly IBranchInventoryService _branchInventoryService;

    public BranchInventoriesController(IBranchInventoryService branchInventoryService)
    {
        _branchInventoryService = branchInventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BranchInventoryDto>>> GetByBranch(int branchId)
    {
        var result = await _branchInventoryService.GetByBranchAsync(branchId);
        return Ok(result);
    }
}
