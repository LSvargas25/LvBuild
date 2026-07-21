using FluentValidation;
using LvApplication.DTOs.Inventory;

namespace LvApplication.Validators.Inventory;

public class UpdateMaterialTicketDtoValidator : AbstractValidator<UpdateMaterialTicketDto>
{
    public UpdateMaterialTicketDtoValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.MaterialId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0).When(x => x.Discount.HasValue);
    }
}
