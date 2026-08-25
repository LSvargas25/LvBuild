using LvApi.Controllers;
using LvApplication.DTOs.Auth;
using LvApplication.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LvApi.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ipAddress);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(request, ipAddress);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.ForgotPassword)]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword(ForgotPasswordRequestDto request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        await _authService.ResetPasswordAsync(request);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var result = await _authService.GetMyProfileAsync(GetCurrentUserId());
        return Ok(result);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileDto request)
    {
        var result = await _authService.UpdateMyProfileAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordDto request)
    {
        await _authService.ChangeMyPasswordAsync(GetCurrentUserId(), request);
        return NoContent();
    }

    [HttpPost("me/photo")]
    public async Task<ActionResult<UserProfileDto>> UploadMyPhoto(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _authService.UpdateMyProfilePhotoAsync(
            GetCurrentUserId(),
            stream,
            file.FileName,
            file.ContentType);

        return Ok(result);
    }
}
