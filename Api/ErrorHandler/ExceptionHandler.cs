using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Api.Observability;
using System.Text.Json;

namespace Api.ErrorHandling;

public sealed class ExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;

    private readonly IProblemDetailsService _problemDetailsService;

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
        var correlationId = Correlation.GetId(httpContext);

        var (statusCode, title, detail) =
            exception switch
            {
                ValidationException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Validation Failed",
                        "One or more request values are invalid."
                    ),

                    AuthenticationFailedException =>
                    (
                        StatusCodes.Status401Unauthorized,
                        "Authentication Failed",
                        exception.Message
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

                DbUpdateException dbUpdateException
                    when IsUniqueConstraintViolation(
                        dbUpdateException) =>
                    (
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        "The request conflicts with existing data."
                    ),

                DbUpdateConcurrencyException =>
                (
                    StatusCodes.Status409Conflict,
                    "Concurrency Conflict",
                    "The resource was modified by another request. Reload it and try again."
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
                        "Something went wrong on our side. An unexpected error occurred."
                    )
            };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception,"Request {CorrelationId} failed with {StatusCode}",correlationId,statusCode);
        }
        else
        {
            _logger.LogInformation("Request {CorrelationId} rejected with {StatusCode} ({ExceptionType})",correlationId,statusCode,exception.GetType().Name);
        }

        ProblemDetails problemDetails;

            if (exception
                is ValidationException validationException)
            {
                var errors =
                    validationException.Errors
                        .ToDictionary(
                            error =>
                                JsonNamingPolicy.CamelCase
                                    .ConvertName(
                                        error.Key),
                            error =>
                                error.Value);

                problemDetails =
                    new ValidationProblemDetails(
                        errors)
                    {
                        Status =
                            StatusCodes.Status400BadRequest,

                        Title =
                            "Validation Failed",

                        Detail =
                            "One or more fields are invalid.",

                        Instance =
                            httpContext.Request.Path
                    };
            }
            else
            {
                problemDetails =
                    new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Detail = detail,
                        Instance =
                            httpContext.Request.Path
                    };
            }

            problemDetails.Extensions[
                "correlationId"] =
                    correlationId;

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

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException
            is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}