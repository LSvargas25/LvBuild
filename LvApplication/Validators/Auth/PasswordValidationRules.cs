using FluentValidation;

namespace LvApplication.Validators.Auth;

public static class PasswordValidationRules
{
    // Shared between ResetPasswordRequestDtoValidator and ChangePasswordDtoValidator so both
    // sides of "I forgot my password" and "I know my password but want to change it" agree.
    public static IRuleBuilderOptions<T, string> MustBeAValidNewPassword<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.NotEmpty().MinimumLength(8);
}
