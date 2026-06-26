using System.Diagnostics;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Api.Middleware;

/// <summary>
/// Single global handler that turns every unhandled exception into an RFC 9457 Problem Details
/// response. Stack traces are logged, never returned to the client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail, field) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Request to {Method} {Path} failed: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://datatracker.ietf.org/doc/html/rfc9457#name-{status}",
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (field is not null)
        {
            problemDetails.Extensions["field"] = field;
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private static (int Status, string Title, string Detail, string? Field) Map(Exception exception) => exception switch
    {
        NotFoundException notFound =>
            (StatusCodes.Status404NotFound, "Resource not found", notFound.Message, null),
        DomainValidationException validation =>
            (StatusCodes.Status400BadRequest, "Validation failed", validation.Message, validation.Field),
        DbUpdateConcurrencyException =>
            (StatusCodes.Status409Conflict, "Concurrency conflict",
                "The resource was modified by another request. Reload and try again.", null),
        OperationCanceledException =>
            (499, "Request cancelled", "The request was cancelled.", null),
        _ =>
            (StatusCodes.Status500InternalServerError, "An unexpected error occurred",
                "An unexpected error occurred while processing the request.", null),
    };
}
