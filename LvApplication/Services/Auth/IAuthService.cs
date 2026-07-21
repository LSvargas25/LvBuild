using LvApplication.DTOs.Auth;

namespace LvApplication.Services.Auth;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null);
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress = null);
    Task RevokeTokenAsync(string refreshToken);
    Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task ResetPasswordAsync(ResetPasswordRequestDto request);

    Task<UserProfileDto> GetMyProfileAsync(int userId);
    Task<UserProfileDto> UpdateMyProfileAsync(int userId, UpdateProfileDto request);
    Task ChangeMyPasswordAsync(int userId, ChangePasswordDto request);
    Task<UserProfileDto> UpdateMyProfilePhotoAsync(int userId, Stream fileStream, string fileName, string contentType);
}
