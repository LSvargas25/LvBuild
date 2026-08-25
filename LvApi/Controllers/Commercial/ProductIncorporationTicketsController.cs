using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Commercial;
using LvApplication.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Commercial;

[ApiController]
[Route("api/product-incorporation-tickets")]
[Authorize]
public class ProductIncorporationTicketsController : ApiControllerBase
{
    private readonly IProductIncorporationTicketService _ticketService;

    public ProductIncorporationTicketsController(IProductIncorporationTicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    public async Task<ActionResult<ProductIncorporationTicketDto>> Create(
        CreateProductIncorporationTicketDto request
    )
    {
        var result = await _ticketService.CreateAsync(
            request,
            GetCurrentUserId(),
            GetCurrentRoles()
        );
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,BranchAdmin")]
    [HttpPost("{id:int}/validate")]
    public async Task<ActionResult<ProductIncorporationTicketDto>> Validate(
        int id,
        ValidateProductIncorporationTicketDto request
    )
    {
        var result = await _ticketService.ValidateAsync(
            id,
            request.Approve,
            GetCurrentUserId(),
            GetCurrentRoles()
        );
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,BranchAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _ticketService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductIncorporationTicketDto>> GetById(int id)
    {
        var result = await _ticketService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("~/api/branches/{branchId:int}/product-incorporation-tickets")]
    public async Task<ActionResult<PagedResult<ProductIncorporationTicketDto>>> GetAllByBranch(
        int branchId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _ticketService.GetAllByBranchAsync(branchId, pageNumber, pageSize);
        return Ok(result);
    }
}
