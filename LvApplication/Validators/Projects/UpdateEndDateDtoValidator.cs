using FluentValidation;
using LvApplication.DTOs.Projects;

namespace LvApplication.Validators.Projects;

public class UpdateEndDateDtoValidator : AbstractValidator<UpdateEndDateDto>
{
    public UpdateEndDateDtoValidator()
    {
        RuleFor(x => x.NewEndDate)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("El motivo es obligatorio.");
    }
}
