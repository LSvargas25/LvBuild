using LvDomain.Entities.Auth;

namespace LvApplication.Services.Auth;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task AddAsync(PasswordResetToken passwordResetToken);
    Task UpdateAsync(PasswordResetToken passwordResetToken);
}
