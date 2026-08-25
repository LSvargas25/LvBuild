using FluentValidation;
using LvApplication.DTOs.Projects;

namespace LvApplication.Validators.Projects;

public class AssignWorkerDtoValidator : AbstractValidator<AssignWorkerDto>
{
    public AssignWorkerDtoValidator()
    {
        RuleFor(x => x.WorkerId).GreaterThan(0);
    }
}
