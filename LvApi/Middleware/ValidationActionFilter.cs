using FluentValidation;
using LvApplication.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LvApi.Middleware;

public class ValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (
                context.HttpContext.RequestServices.GetService(validatorType)
                is not IValidator validator
            )
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
                throw new ValidationAppException(message);
            }
        }

        await next();
    }
}
