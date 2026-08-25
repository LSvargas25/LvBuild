using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Payroll;
using LvApplication.Services.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Payroll;

[ApiController]
[Route("api")]
[Authorize]
public class PayrollsController : ApiControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollsController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("payrolls")]
    public async Task<ActionResult<PayrollDto>> Create(CreatePayrollDto request)
    {
        var result = await _payrollService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("payrolls/{id:int}")]
    public async Task<ActionResult<PayrollDto>> Update(int id, UpdatePayrollDto request)
    {
        var result = await _payrollService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("payrolls/{id:int}/mark-paid")]
    public async Task<ActionResult<PayrollDto>> MarkAsPaid(int id)
    {
        var result = await _payrollService.MarkAsPaidAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("payrolls/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _payrollService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("payrolls")]
    public async Task<ActionResult<PagedResult<PayrollDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _payrollService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("payrolls/{id:int}")]
    public async Task<ActionResult<PayrollDto>> GetById(int id)
    {
        var result = await _payrollService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("projects/{projectId:int}/payrolls")]
    public async Task<ActionResult<PagedResult<PayrollDto>>> GetAllByProject(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _payrollService.GetAllByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }
}
