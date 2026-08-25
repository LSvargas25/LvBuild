using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Offers;
using LvApplication.Services.Offers;
using LvDomain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Offers;

[ApiController]
[Route("api/offers")]
[Authorize]
public class OffersController : ApiControllerBase
{
    private readonly IOfferService _offerService;

    public OffersController(IOfferService offerService)
    {
        _offerService = offerService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost]
    public async Task<ActionResult<OfferResponseDto>> Create(CreateOfferDto request)
    {
        var result = await _offerService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OfferResponseDto>> Update(int id, UpdateOfferDto request)
    {
        var result = await _offerService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("{id:int}/send-to-client")]
    public async Task<ActionResult<OfferResponseDto>> SendToClient(int id)
    {
        var result = await _offerService.SendToClientAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/mark-accepted")]
    public async Task<ActionResult<OfferResponseDto>> MarkAccepted(int id)
    {
        var result = await _offerService.MarkAcceptedAsync(id, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/revert-to-draft")]
    public async Task<ActionResult<OfferResponseDto>> RevertToDraft(int id, RevertToDraftDto request)
    {
        var result = await _offerService.RevertToDraftAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _offerService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var (filePath, fileName) = await _offerService.GetPdfFileAsync(id);
        return PhysicalFile(Path.GetFullPath(filePath), "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OfferResponseDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OfferStatus? status = null)
    {
        var result = await _offerService.GetAllAsync(pageNumber, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OfferResponseDto>> GetById(int id)
    {
        var result = await _offerService.GetByIdAsync(id);
        return Ok(result);
    }
}
