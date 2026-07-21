using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class PayrollDetailDtoValidator : AbstractValidator<PayrollDetailDto>
{
    public PayrollDetailDtoValidator()
    {
        RuleFor(d => d.WorkerId).GreaterThan(0);
        RuleFor(d => d.HoursWorked).GreaterThan(0);
        RuleFor(d => d.HourlyRate).GreaterThanOrEqualTo(0);

        RuleFor(d => d)
            .Must(d =>
            {
                var finalAmountToPay = (d.HoursWorked * d.HourlyRate) - (d.AdvanceAmountApplied ?? 0);
                return d.Payments.Sum(p => p.Amount) == finalAmountToPay;
            })
            .WithMessage("La suma de los pagos de cada detalle debe coincidir con el monto final a pagar.");
    }
}
