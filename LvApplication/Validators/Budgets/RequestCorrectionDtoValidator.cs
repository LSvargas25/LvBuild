using FluentValidation;
using LvApplication.DTOs.Budgets;

namespace LvApplication.Validators.Budgets;

public class RequestCorrectionDtoValidator : AbstractValidator<RequestCorrectionDto>
{
    public RequestCorrectionDtoValidator()
    {
        RuleFor(x => x.Comment)
            .NotEmpty()
            .WithMessage("El comentario es obligatorio.");
    }
}
