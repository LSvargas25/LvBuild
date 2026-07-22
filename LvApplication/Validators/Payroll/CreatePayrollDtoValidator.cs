using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class CreatePayrollDtoValidator : AbstractValidator<CreatePayrollDto>
{
    public CreatePayrollDtoValidator()
    {
        RuleFor(x => x.SiteLogId).GreaterThan(0);
        RuleFor(x => x.ChapterId).GreaterThan(0).When(x => x.ChapterId.HasValue);

        RuleForEach(x => x.Details).SetValidator(new PayrollDetailDtoValidator());
    }
}
