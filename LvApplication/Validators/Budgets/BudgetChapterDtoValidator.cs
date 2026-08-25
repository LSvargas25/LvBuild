using FluentValidation;
using LvApplication.DTOs.Budgets;

namespace LvApplication.Validators.Budgets;

public class BudgetChapterDtoValidator : AbstractValidator<BudgetChapterDto>
{
    public BudgetChapterDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();

        RuleForEach(x => x.Activities).SetValidator(new BudgetActivityDtoValidator());
    }
}
