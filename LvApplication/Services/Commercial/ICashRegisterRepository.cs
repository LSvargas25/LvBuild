using LvDomain.Entities.Commercial;

namespace LvApplication.Services.Commercial;

public interface ICashRegisterRepository
{
    Task<CashRegister?> GetByIdAsync(int id);
    Task<CashRegister?> GetOpenByBranchAsync(int branchId);
    Task AddAsync(CashRegister cashRegister);
    Task UpdateAsync(CashRegister cashRegister);
    Task DeleteAsync(CashRegister cashRegister);
    Task<bool> HasInvoicesAsync(int cashRegisterId);
    Task<decimal> SumCashPaymentsAsync(int cashRegisterId);
}
