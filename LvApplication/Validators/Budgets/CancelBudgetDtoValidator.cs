using FluentValidation;
using LvApplication.DTOs.Budgets;

namespace LvApplication.Validators.Budgets;

public class CancelBudgetDtoValidator : AbstractValidator<CancelBudgetDto>
{
    public CancelBudgetDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("El motivo de la cancelación es obligatorio.");
    }
}
