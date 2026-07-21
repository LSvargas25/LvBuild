using FluentValidation;
using LvApplication.DTOs.Auth;

namespace LvApplication.Validators.Auth;

public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();
    }
}
