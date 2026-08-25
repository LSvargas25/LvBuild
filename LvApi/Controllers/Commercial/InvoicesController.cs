using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Commercial;
using LvApplication.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Commercial;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ApiControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceDto request)
    {
        var result = await _invoiceService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> UpdateDraft(int id, UpdateInvoiceDraftDto request)
    {
        var result = await _invoiceService.UpdateDraftAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:int}/issue")]
    public async Task<ActionResult<InvoiceDto>> Issue(int id, IssueInvoiceDto request)
    {
        var result = await _invoiceService.IssueAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<InvoiceDto>> AddPayment(int id, CreateInvoicePaymentDto request)
    {
        var result = await _invoiceService.AddPaymentAsync(id, request, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<InvoiceDto>> Cancel(int id)
    {
        var result = await _invoiceService.CancelAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,BranchAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _invoiceService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id)
    {
        var result = await _invoiceService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("~/api/branches/{branchId:int}/invoices")]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetAllByBranch(
        int branchId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _invoiceService.GetAllByBranchAsync(branchId, pageNumber, pageSize);
        return Ok(result);
    }
}
