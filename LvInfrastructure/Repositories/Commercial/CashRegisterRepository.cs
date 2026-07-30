using LvApplication.Services.Commercial;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Commercial;

public class CashRegisterRepository : ICashRegisterRepository
{
    private readonly AppDbContext _context;

    public CashRegisterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CashRegister?> GetByIdAsync(int id) =>
        await _context.CashRegisters.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CashRegister?> GetOpenByBranchAsync(int branchId) =>
        await _context.CashRegisters.FirstOrDefaultAsync(c => c.BranchId == branchId && c.Status == CashRegisterStatus.Open);

    public async Task AddAsync(CashRegister register)
    {
        _context.CashRegisters.Add(register);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CashRegister register)
    {
        _context.CashRegisters.Update(register);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(CashRegister register)
    {
        _context.CashRegisters.Remove(register);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasInvoicesAsync(int cashRegisterId) =>
        await _context.Invoices.AnyAsync(i => i.CashRegisterId == cashRegisterId);

    public async Task<decimal> SumCashPaymentsAsync(int cashRegisterId) =>
        await _context.InvoicePayments
            .Where(p => p.PaymentMethod == InvoicePaymentMethod.Efectivo && p.Invoice.CashRegisterId == cashRegisterId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
}
