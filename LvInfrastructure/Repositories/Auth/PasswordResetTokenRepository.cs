using LvApplication.Services.Auth;
using LvDomain.Entities.Auth;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Auth;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _context;

    public PasswordResetTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<PasswordResetToken?> GetByTokenAsync(string token) =>
        _context
            .PasswordResetTokens.Include(prt => prt.User)
            .FirstOrDefaultAsync(prt => prt.Token == token);

    public async Task AddAsync(PasswordResetToken passwordResetToken)
    {
        _context.PasswordResetTokens.Add(passwordResetToken);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PasswordResetToken passwordResetToken)
    {
        _context.PasswordResetTokens.Update(passwordResetToken);
        await _context.SaveChangesAsync();
    }
}
