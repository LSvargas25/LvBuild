using FluentValidation;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Validators.Commercial;

public class CreateProductIncorporationTicketDtoValidator : AbstractValidator<CreateProductIncorporationTicketDto>
{
    public CreateProductIncorporationTicketDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}
