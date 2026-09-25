using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Api.Exceptions;

namespace Api.ErrorHandling;

public sealed class ExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ExceptionHandler>
        _logger;

    private readonly IProblemDetailsService
        _problemDetailsService;

    public ExceptionHandler(
        ILogger<ExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId =
            httpContext.TraceIdentifier;

        var (statusCode, title, detail) =
            exception switch
            {
                RequestValidationException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Validation Failed",
                        "One or more request values are invalid."
                    ),

                ResourceNotFoundException =>
                    (
                        StatusCodes.Status404NotFound,
                        "Resource Not Found",
                        exception.Message
                    ),

                ConflictException =>
                    (
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        exception.Message
                    ),

                BusinessRuleException =>
                    (
                        StatusCodes.Status422UnprocessableEntity,
                        "Business Rule Violation",
                        exception.Message
                    ),

                _ =>
                    (
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        "An unexpected error occurred."
                    )
            };

        // Use structured logging so the correlation ID
        // can be searched independently in the logs.
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Request failed with status {StatusCode}. CorrelationId={CorrelationId}",
                statusCode,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status {StatusCode}. CorrelationId={CorrelationId}",
                statusCode,
                correlationId);
        }

        var problemDetails =
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance =
                    httpContext.Request.Path
            };

        // Same identifier appears in the response and log.
        problemDetails.Extensions[
            "correlationId"] = correlationId;

        // Add field-by-field validation errors when relevant.
        if (exception
            is RequestValidationException validationException)
        {
            problemDetails.Extensions["errors"] =
                validationException.Errors;
        }

        httpContext.Response.StatusCode =
            statusCode;

        httpContext.Response.ContentType =
            "application/problem+json";

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
    }
}