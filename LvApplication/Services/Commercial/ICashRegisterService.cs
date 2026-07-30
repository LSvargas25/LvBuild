using LvApplication.DTOs.Commercial;

namespace LvApplication.Services.Commercial;

public interface ICashRegisterService
{
    Task<CashRegisterDto> OpenAsync(OpenCashRegisterDto request, int openedByUserId);
    Task<CashRegisterDto> CloseAsync(int id, CloseCashRegisterDto request, int closedByUserId);
    Task DeleteAsync(int id);
    Task<CashRegisterDto> GetByIdAsync(int id);
}
