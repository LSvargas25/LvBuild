using FluentValidation;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Validators.Commercial;

public class CloseCashRegisterDtoValidator : AbstractValidator<CloseCashRegisterDto>
{
    public CloseCashRegisterDtoValidator()
    {
        RuleFor(x => x.ClosingBalance).GreaterThanOrEqualTo(0);
    }
}
