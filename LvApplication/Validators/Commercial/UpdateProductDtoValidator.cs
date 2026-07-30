using FluentValidation;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Validators.Commercial;

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.UnitOfMeasure).MaximumLength(30);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}
