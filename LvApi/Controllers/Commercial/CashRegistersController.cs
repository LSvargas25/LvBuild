using LvApi.Controllers;
using LvApplication.DTOs.Commercial;
using LvApplication.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Commercial;

[ApiController]
[Route("api/cash-registers")]
[Authorize]
public class CashRegistersController : ApiControllerBase
{
    private readonly ICashRegisterService _cashRegisterService;

    public CashRegistersController(ICashRegisterService cashRegisterService)
    {
        _cashRegisterService = cashRegisterService;
    }

    [HttpPost("open")]
    public async Task<ActionResult<CashRegisterDto>> Open(OpenCashRegisterDto request)
    {
        var result = await _cashRegisterService.OpenAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<CashRegisterDto>> Close(int id, CloseCashRegisterDto request)
    {
        var result = await _cashRegisterService.CloseAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,BranchAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _cashRegisterService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CashRegisterDto>> GetById(int id)
    {
        var result = await _cashRegisterService.GetByIdAsync(id);
        return Ok(result);
    }
}
