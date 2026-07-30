using FluentValidation;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Validators.Commercial;

public class CreateInvoicePaymentDtoValidator : AbstractValidator<CreateInvoicePaymentDto>
{
    public CreateInvoicePaymentDtoValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
