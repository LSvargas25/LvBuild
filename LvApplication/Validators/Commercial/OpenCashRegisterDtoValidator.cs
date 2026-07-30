using FluentValidation;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Validators.Commercial;

public class OpenCashRegisterDtoValidator : AbstractValidator<OpenCashRegisterDto>
{
    public OpenCashRegisterDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}
