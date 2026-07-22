using FluentValidation;
using LvApplication.DTOs.Projects;

namespace LvApplication.Validators.Projects;

public class UpdateAssignedSoldTotalDtoValidator : AbstractValidator<UpdateAssignedSoldTotalDto>
{
    public UpdateAssignedSoldTotalDtoValidator()
    {
        RuleFor(x => x.AssignedSoldTotal).GreaterThanOrEqualTo(0);
    }
}
