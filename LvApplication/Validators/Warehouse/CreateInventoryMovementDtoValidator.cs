using FluentValidation;
using LvApplication.DTOs.Warehouse;

namespace LvApplication.Validators.Warehouse;

public class CreateInventoryMovementDtoValidator : AbstractValidator<CreateInventoryMovementDto>
{
    public CreateInventoryMovementDtoValidator()
    {
        RuleFor(x => x.OriginBranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);

        RuleFor(x => x.DestinationBranchId)
            .GreaterThan(0)
            .When(x => x.DestinationBranchId.HasValue);
        RuleFor(x => x.DestinationProjectId)
            .GreaterThan(0)
            .When(x => x.DestinationProjectId.HasValue);

        RuleFor(x => x)
            .Must(x =>
                (x.DestinationBranchId.HasValue && !x.DestinationProjectId.HasValue)
                || (!x.DestinationBranchId.HasValue && x.DestinationProjectId.HasValue)
            )
            .WithMessage(
                "Debe indicar exactamente un destino: sucursal o proyecto, no ambos ni ninguno."
            )
            .WithName(nameof(CreateInventoryMovementDto.DestinationBranchId));
    }
}
