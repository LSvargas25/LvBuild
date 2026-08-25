using System.Security.Claims;
using LvApplication.Common;
using LvApplication.DTOs.Auth;
using LvApplication.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Auth;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize(Roles = "GeneralManager")]
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> Create(CreateUserDto request)
    {
        var actingUserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var result = await _userService.CreateUserAsync(request, actingUserRoles);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserResponseDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _userService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }
}
