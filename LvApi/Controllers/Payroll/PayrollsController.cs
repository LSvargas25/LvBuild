using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Payroll;
using LvApplication.Services.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Payroll;

/// <summary>Weekly payrolls generated from approved site logs.</summary>
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

    /// <summary>Creates a Pending payroll from an approved site log.</summary>
    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("payrolls")]
    public async Task<ActionResult<PayrollDto>> Create(CreatePayrollDto request)
    {
        var result = await _payrollService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates a Pending payroll.</summary>
    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("payrolls/{id:int}")]
    public async Task<ActionResult<PayrollDto>> Update(int id, UpdatePayrollDto request)
    {
        var result = await _payrollService.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Pending -&gt; Paid; adds the payroll total to the project's direct expenses.</summary>
    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("payrolls/{id:int}/mark-paid")]
    public async Task<ActionResult<PayrollDto>> MarkAsPaid(int id)
    {
        var result = await _payrollService.MarkAsPaidAsync(id);
        return Ok(result);
    }

    /// <summary>Deletes a payroll.</summary>
    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("payrolls/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _payrollService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Lists payrolls (paged).</summary>
    [HttpGet("payrolls")]
    public async Task<ActionResult<PagedResult<PayrollDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _payrollService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>Returns a payroll with its details and payments.</summary>
    [HttpGet("payrolls/{id:int}")]
    public async Task<ActionResult<PayrollDto>> GetById(int id)
    {
        var result = await _payrollService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Lists the payrolls of a project (paged).</summary>
    [HttpGet("projects/{projectId:int}/payrolls")]
    public async Task<ActionResult<PagedResult<PayrollDto>>> GetAllByProject(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _payrollService.GetAllByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }
}
