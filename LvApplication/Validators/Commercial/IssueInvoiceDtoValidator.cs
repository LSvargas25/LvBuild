using FluentValidation;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Validators.Commercial;

public class IssueInvoiceDtoValidator : AbstractValidator<IssueInvoiceDto>
{
    public IssueInvoiceDtoValidator()
    {
        RuleForEach(x => x.Payments)
            .ChildRules(payment =>
            {
                payment.RuleFor(p => p.Amount).GreaterThan(0);
            });
    }
}
