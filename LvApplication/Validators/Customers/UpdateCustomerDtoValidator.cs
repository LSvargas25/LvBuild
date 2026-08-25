using FluentValidation;
using LvApplication.DTOs.Customers;

namespace LvApplication.Validators.Customers;

public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();

        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CustomerType).IsInEnum();

        RuleFor(x => x.Status).IsInEnum();
    }
}
