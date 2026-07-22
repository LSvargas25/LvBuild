using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class UpdatePayrollDtoValidator : AbstractValidator<UpdatePayrollDto>
{
    public UpdatePayrollDtoValidator()
    {
        RuleFor(x => x.ChapterId).GreaterThan(0).When(x => x.ChapterId.HasValue);

        RuleForEach(x => x.Details).SetValidator(new PayrollDetailDtoValidator());
    }
}
