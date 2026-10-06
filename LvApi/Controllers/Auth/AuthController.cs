using LvApi.Controllers;
using LvApplication.DTOs.Auth;
using LvApplication.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LvApi.Controllers.Auth;

/// <summary>Authentication: JWT login with refresh-token rotation, password reset and the current user's profile.</summary>
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

    /// <summary>Logs in with email and password. Returns an access token (Bearer) and a refresh token. Rate limited to 5 attempts per minute per IP; the account is blocked after 5 consecutive failures.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ipAddress);
        return Ok(result);
    }

    /// <summary>Exchanges a valid refresh token for a new access/refresh token pair (the old refresh token is revoked).</summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(request, ipAddress);
        return Ok(result);
    }

    /// <summary>Revokes the given refresh token.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return NoContent();
    }

    /// <summary>Starts the password reset flow. Always answers 200 so it cannot be used to discover registered emails.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.ForgotPassword)]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword(
        ForgotPasswordRequestDto request
    )
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    /// <summary>Sets a new password using a reset token.</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        await _authService.ResetPasswordAsync(request);
        return NoContent();
    }

    /// <summary>Returns the profile of the authenticated user.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var result = await _authService.GetMyProfileAsync(GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>Updates the name of the authenticated user.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileDto request)
    {
        var result = await _authService.UpdateMyProfileAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    /// <summary>Changes the password of the authenticated user (requires the current password).</summary>
    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordDto request)
    {
        await _authService.ChangeMyPasswordAsync(GetCurrentUserId(), request);
        return NoContent();
    }

    /// <summary>Uploads the authenticated user's profile photo (JPEG, PNG or WEBP, max 1 MB).</summary>
    [HttpPost("me/photo")]
    public async Task<ActionResult<UserProfileDto>> UploadMyPhoto(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _authService.UpdateMyProfilePhotoAsync(
            GetCurrentUserId(),
            stream,
            file.FileName,
            file.ContentType
        );

        return Ok(result);
    }
}
