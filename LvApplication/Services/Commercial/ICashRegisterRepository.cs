using LvDomain.Entities.Commercial;

namespace LvApplication.Services.Commercial;

public interface ICashRegisterRepository
{
    Task<CashRegister?> GetByIdAsync(int id);
    Task<CashRegister?> GetOpenByBranchAsync(int branchId);
    Task AddAsync(CashRegister register);
    Task UpdateAsync(CashRegister register);
    Task DeleteAsync(CashRegister register);
    Task<bool> HasInvoicesAsync(int cashRegisterId);
    Task<decimal> SumCashPaymentsAsync(int cashRegisterId);
}
