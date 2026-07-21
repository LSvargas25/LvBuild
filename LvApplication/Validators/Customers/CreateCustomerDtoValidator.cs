using FluentValidation;
using LvApplication.DTOs.Customers;

namespace LvApplication.Validators.Customers;

public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CustomerType)
            .IsInEnum();
    }
}
