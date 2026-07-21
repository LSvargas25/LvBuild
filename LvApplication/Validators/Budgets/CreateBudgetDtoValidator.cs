using FluentValidation;
using LvApplication.DTOs.Budgets;
using LvApplication.Services.Customers;
using LvDomain.Enums;

namespace LvApplication.Validators.Budgets;

public class CreateBudgetDtoValidator : AbstractValidator<CreateBudgetDto>
{
    public CreateBudgetDtoValidator(ICustomerRepository customerRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.CustomerId)
            .MustAsync(async (customerId, _) =>
            {
                var customer = await customerRepository.GetByIdAsync(customerId);
                return customer is not null && customer.CustomerType == CustomerType.Project;
            })
            .WithMessage("El presupuesto solo puede asociarse a un cliente de tipo Proyecto");

        RuleForEach(x => x.Chapters)
            .SetValidator(new BudgetChapterDtoValidator());
    }
}
