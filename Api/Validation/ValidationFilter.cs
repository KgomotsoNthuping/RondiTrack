using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Api.Exceptions;

namespace Api.Validation;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var errors =
            new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

        // Capture JSON/model-binding failures.
        foreach (var modelStateEntry in context.ModelState)
        {
            foreach (var error in modelStateEntry.Value.Errors)
            {
                AddError(
                    errors,
                    modelStateEntry.Key,
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The supplied value is invalid."
                        : error.ErrorMessage);
            }
        }

        // Run the FluentValidation validator for every request DTO.
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType =
                typeof(IValidator<>)
                    .MakeGenericType(argument.GetType());

            var validator =
                context.HttpContext
                    .RequestServices
                    .GetService(validatorType)
                as IValidator;

            if (validator is null)
            {
                continue;
            }

            var validationContext =
                new ValidationContext<object>(argument);

            var validationResult =
                await validator.ValidateAsync(
                    validationContext,
                    context.HttpContext.RequestAborted);

            foreach (var failure in validationResult.Errors)
            {
                AddError(
                    errors,
                    failure.PropertyName,
                    failure.ErrorMessage);
            }
        }

        // Idempotency-Key is part of the request contract,
        // but it is a header instead of a DTO property.
        if (context.ActionDescriptor
                is ControllerActionDescriptor descriptor &&
            descriptor.MethodInfo
                .GetCustomAttribute<
                    RequireIdempotencyKeyAttribute>() is not null)
        {
            var key =
                context.HttpContext.Request.Headers[
                    "Idempotency-Key"];

            if (string.IsNullOrWhiteSpace(key))
            {
                AddError(
                    errors,
                    "Idempotency-Key",
                    "Idempotency-Key header is required.");
            }
        }

        if (errors.Count > 0)
        {
            var formattedErrors =
                errors.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value
                        .Distinct()
                        .ToArray());

            throw new RequestValidationException(
                formattedErrors);
        }

        await next();
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string property,
        string message)
    {
        if (!errors.TryGetValue(
                property,
                out var messages))
        {
            messages = [];
            errors[property] = messages;
        }

        messages.Add(message);
    }
}