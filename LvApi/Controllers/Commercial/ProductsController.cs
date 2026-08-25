using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Commercial;
using LvApplication.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Commercial;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto request)
    {
        var result = await _productService.CreateAsync(
            request,
            GetCurrentUserId(),
            GetCurrentRoles()
        );
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductDto request)
    {
        var result = await _productService.UpdateAsync(id, request, GetCurrentRoles());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,BranchAdmin")]
    [HttpPost("{id:int}/validate")]
    public async Task<ActionResult<ProductDto>> Validate(int id, ValidateProductDto request)
    {
        var result = await _productService.ValidateAsync(
            id,
            request.Approve,
            GetCurrentUserId(),
            GetCurrentRoles()
        );
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<ProductDto>> Deactivate(int id)
    {
        var result = await _productService.DeactivateAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<ProductDto>> Activate(int id)
    {
        var result = await _productService.ActivateAsync(id);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = true
    )
    {
        var result = await _productService.GetAllAsync(pageNumber, pageSize, activeOnly);
        return Ok(result);
    }
}
