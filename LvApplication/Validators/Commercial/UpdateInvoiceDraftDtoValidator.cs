using FluentValidation;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Validators.Commercial;

public class UpdateInvoiceDraftDtoValidator : AbstractValidator<UpdateInvoiceDraftDto>
{
    public UpdateInvoiceDraftDtoValidator()
    {
        RuleFor(x => x.Details).NotEmpty().WithMessage("La factura debe tener al menos una línea.");
        RuleForEach(x => x.Details)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.ProductId).GreaterThan(0);
                line.RuleFor(l => l.Quantity).GreaterThan(0);
            });
    }
}
