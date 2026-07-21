using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Auth;
using LvDomain.Entities.Auth;
using LvDomain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LvApplication.Services.Auth;

public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<RefreshTokenRequestDto> _refreshTokenValidator;
    private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RefreshTokenRequestDto> refreshTokenValidator,
        IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordValidator)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null)
    {
        await _loginValidator.ValidateAndThrowAppExceptionAsync(request);

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
        {
            throw new ForbiddenException(InvalidCredentialsMessage);
        }

        if (user.Status == UserStatus.Blocked)
        {
            throw new ForbiddenException("This account is blocked. Contact an administrator.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await RegisterFailedLoginAttemptAsync(user);
            throw new ForbiddenException(InvalidCredentialsMessage);
        }

        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return await IssueTokensAsync(user, ipAddress);
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress = null)
    {
        await _refreshTokenValidator.ValidateAndThrowAppExceptionAsync(request);

        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (existingToken is null || existingToken.Revoked || existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new ForbiddenException("Invalid or expired refresh token.");
        }

        existingToken.Revoked = true;
        existingToken.UpdatedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(existingToken);

        var user = existingToken.User ?? await _userRepository.GetByIdAsync(existingToken.UserId)
            ?? throw new NotFoundException("User not found.");

        return await IssueTokensAsync(user, ipAddress);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (token is null || token.Revoked)
        {
            return;
        }

        token.Revoked = true;
        token.UpdatedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(token);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        await _forgotPasswordValidator.ValidateAndThrowAppExceptionAsync(request);

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
        {
            return;
        }

        var expirationMinutes = _configuration.GetValue<int?>("Security:PasswordResetTokenExpirationMinutes") ?? 30;
        var token = _tokenService.GenerateRefreshToken();

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            CreatedAt = DateTime.UtcNow
        };

        await _passwordResetTokenRepository.AddAsync(resetToken);

        _logger.LogInformation("Password reset token generated for user {UserId}: {Token}", user.Id, token);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        await _resetPasswordValidator.ValidateAndThrowAppExceptionAsync(request);

        var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(request.Token);
        if (resetToken is null || resetToken.Used || resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new ValidationAppException("Invalid or expired password reset token.");
        }

        var user = resetToken.User ?? await _userRepository.GetByIdAsync(resetToken.UserId)
            ?? throw new NotFoundException("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        resetToken.Used = true;
        resetToken.UpdatedAt = DateTime.UtcNow;
        await _passwordResetTokenRepository.UpdateAsync(resetToken);

        await _refreshTokenRepository.RevokeAllActiveTokensForUserAsync(user.Id);
    }

    private async Task RegisterFailedLoginAttemptAsync(User user)
    {
        var maxFailedAttempts = _configuration.GetValue<int?>("Security:MaxFailedLoginAttempts") ?? 5;

        user.FailedLoginAttempts += 1;

        if (user.FailedLoginAttempts >= maxFailedAttempts)
        {
            user.Status = UserStatus.Blocked;
            user.BlockedAt = DateTime.UtcNow;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }

    private async Task<LoginResponseDto> IssueTokensAsync(User user, string? ipAddress)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _tokenService.GetRefreshTokenExpiresAt(),
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken);

        return new LoginResponseDto
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Roles = roles
        };
    }
}
