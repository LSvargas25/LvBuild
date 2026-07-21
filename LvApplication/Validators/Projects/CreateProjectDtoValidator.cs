using FluentValidation;
using LvApplication.DTOs.Projects;

namespace LvApplication.Validators.Projects;

public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectDtoValidator()
    {
        RuleFor(x => x.OfferId)
            .GreaterThan(0);

        RuleFor(x => x.BranchId)
            .GreaterThan(0);

        RuleFor(x => x.StartDate)
            .NotEmpty();
    }
}
