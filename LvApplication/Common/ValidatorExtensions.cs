using FluentValidation;
using LvApplication.Common.Exceptions;

namespace LvApplication.Common;

public static class ValidatorExtensions
{
    public static async Task ValidateAndThrowAppExceptionAsync<T>(this IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);

        if (!result.IsValid)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
            throw new ValidationAppException(message);
        }
    }
}
