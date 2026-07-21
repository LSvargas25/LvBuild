using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class CreatePayrollDtoValidator : AbstractValidator<CreatePayrollDto>
{
    public CreatePayrollDtoValidator()
    {
        RuleFor(x => x.SiteLogId).GreaterThan(0);

        RuleForEach(x => x.Details).SetValidator(new PayrollDetailDtoValidator());
    }
}
