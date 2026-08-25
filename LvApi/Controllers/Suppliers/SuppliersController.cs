using LvApplication.Common;
using LvApplication.DTOs.Suppliers;
using LvApplication.Services.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Suppliers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = "GeneralManager,OperationsDirector")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpPost]
    public async Task<ActionResult<SupplierResponseDto>> Create(CreateSupplierDto request)
    {
        var result = await _supplierService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierResponseDto>> Update(int id, UpdateSupplierDto request)
    {
        var result = await _supplierService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierResponseDto>> GetById(int id)
    {
        var result = await _supplierService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierResponseDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _supplierService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _supplierService.DeleteAsync(id);
        return NoContent();
    }
}
