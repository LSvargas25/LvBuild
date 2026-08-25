using FluentValidation;
using LvApplication.DTOs.Budgets;

namespace LvApplication.Validators.Budgets;

public class BudgetActivityDtoValidator : AbstractValidator<BudgetActivityDto>
{
    public BudgetActivityDtoValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
    }
}
