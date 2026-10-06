using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Commercial;
using LvApplication.Services.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;

namespace LvApplication.Services.Commercial;

public class CashRegisterService : ICashRegisterService
{
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IValidator<OpenCashRegisterDto> _openValidator;
    private readonly IValidator<CloseCashRegisterDto> _closeValidator;

    public CashRegisterService(
        ICashRegisterRepository cashRegisterRepository,
        IBranchRepository branchRepository,
        IValidator<OpenCashRegisterDto> openValidator,
        IValidator<CloseCashRegisterDto> closeValidator
    )
    {
        _cashRegisterRepository = cashRegisterRepository;
        _branchRepository = branchRepository;
        _openValidator = openValidator;
        _closeValidator = closeValidator;
    }

    public async Task<CashRegisterDto> OpenAsync(OpenCashRegisterDto request, int openedByUserId)
    {
        await _openValidator.ValidateAndThrowAppExceptionAsync(request);

        var branch =
            await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new NotFoundException($"No se encontró la sucursal {request.BranchId}.");

        if (branch.BranchType != BranchType.Commercial)
        {
            throw new ValidationAppException(
                "Solo se puede abrir una caja en una sucursal de tipo Comercio."
            );
        }

        var existingOpen = await _cashRegisterRepository.GetOpenByBranchAsync(request.BranchId);
        if (existingOpen is not null)
        {
            throw new ConflictException(
                $"La sucursal {request.BranchId} ya tiene una caja abierta."
            );
        }

        var now = DateTime.UtcNow;
        var register = new CashRegister
        {
            BranchId = request.BranchId,
            OpenedByUserId = openedByUserId,
            OpeningDate = now,
            OpeningBalance = request.OpeningBalance,
            Status = CashRegisterStatus.Open,
            CreatedAt = now,
        };

        await _cashRegisterRepository.AddAsync(register);

        return MapToDto(register);
    }

    public async Task<CashRegisterDto> CloseAsync(
        int id,
        CloseCashRegisterDto request,
        int closedByUserId
    )
    {
        await _closeValidator.ValidateAndThrowAppExceptionAsync(request);

        var register =
            await _cashRegisterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la caja {id}.");

        if (register.Status != CashRegisterStatus.Open)
        {
            throw new ValidationAppException("Solo se puede cerrar una caja en estado Open.");
        }

        var cashSales = await _cashRegisterRepository.SumCashPaymentsAsync(id);
        var expectedBalance = register.OpeningBalance + cashSales;

        register.ExpectedBalance = expectedBalance;
        register.ClosingBalance = request.ClosingBalance;
        register.Difference = request.ClosingBalance - expectedBalance;
        register.ClosedByUserId = closedByUserId;
        register.ClosingDate = DateTime.UtcNow;
        register.Status = CashRegisterStatus.Closed;
        register.UpdatedAt = DateTime.UtcNow;

        await _cashRegisterRepository.UpdateAsync(register);

        return MapToDto(register);
    }

    public async Task DeleteAsync(int id)
    {
        var register =
            await _cashRegisterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la caja {id}.");

        if (register.Status != CashRegisterStatus.Open)
        {
            throw new ValidationAppException("Solo se puede eliminar una caja en estado Open.");
        }

        if (await _cashRegisterRepository.HasInvoicesAsync(id))
        {
            throw new ValidationAppException(
                "No se puede eliminar una caja con facturas asociadas."
            );
        }

        await _cashRegisterRepository.DeleteAsync(register);
    }

    public async Task<CashRegisterDto> GetByIdAsync(int id)
    {
        var register =
            await _cashRegisterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la caja {id}.");
        return MapToDto(register);
    }

    private static CashRegisterDto MapToDto(CashRegister register) =>
        new()
        {
            Id = register.Id,
            BranchId = register.BranchId,
            OpenedByUserId = register.OpenedByUserId,
            OpeningDate = register.OpeningDate,
            OpeningBalance = register.OpeningBalance,
            Status = register.Status,
            ClosedByUserId = register.ClosedByUserId,
            ClosingDate = register.ClosingDate,
            ClosingBalance = register.ClosingBalance,
            ExpectedBalance = register.ExpectedBalance,
            Difference = register.Difference,
        };
}
