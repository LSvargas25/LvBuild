using FluentValidation;
using LvApplication.DTOs.Auth;

namespace LvApplication.Validators.Auth;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .MustBeAValidNewPassword();
    }
}
