using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SalesDashboard.Api.Filters;

/// <summary>
/// Runs the FluentValidation validator (if one is registered) for every action argument and answers
/// 400 with an RFC 7807 validation problem, so controllers and services never see invalid input.
/// Binding errors (e.g. an unparsable date) are already rejected earlier by [ApiController].
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            foreach (var group in result.Errors.GroupBy(e => ToCamelCase(e.PropertyName)))
            {
                errors[group.Key] = group.Select(e => e.ErrorMessage).Distinct().ToArray();
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
            });
            return;
        }

        await next();
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
