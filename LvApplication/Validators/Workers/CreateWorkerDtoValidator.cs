using FluentValidation;
using LvApplication.DTOs.Workers;

namespace LvApplication.Validators.Workers;

public class CreateWorkerDtoValidator : AbstractValidator<CreateWorkerDto>
{
    public CreateWorkerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => WorkerCategoryTypeMap.IsValidCombination(x.Category, x.Type))
            .WithMessage("El tipo de trabajador no corresponde a la categoría seleccionada")
            .WithName(nameof(CreateWorkerDto.Type));
    }
}
