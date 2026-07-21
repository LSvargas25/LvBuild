using LvApplication.Common;
using LvApplication.DTOs.Workers;
using LvApplication.Services.Workers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Workers;

[ApiController]
[Route("api/workers")]
[Authorize]
public class WorkersController : ControllerBase
{
    private readonly IWorkerService _workerService;

    public WorkersController(IWorkerService workerService)
    {
        _workerService = workerService;
    }

    [HttpPost]
    public async Task<ActionResult<WorkerResponseDto>> Create(CreateWorkerDto request)
    {
        var result = await _workerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkerResponseDto>> Update(int id, UpdateWorkerDto request)
    {
        var result = await _workerService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkerResponseDto>> GetById(int id)
    {
        var result = await _workerService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkerResponseDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _workerService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _workerService.DeleteAsync(id);
        return NoContent();
    }
}
