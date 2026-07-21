using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class UpdatePayrollDtoValidator : AbstractValidator<UpdatePayrollDto>
{
    public UpdatePayrollDtoValidator()
    {
        RuleForEach(x => x.Details).SetValidator(new PayrollDetailDtoValidator());
    }
}
