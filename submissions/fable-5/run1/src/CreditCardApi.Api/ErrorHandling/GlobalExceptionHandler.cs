using CreditCardApi.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Api.ErrorHandling;

/// <summary>
/// Single global exception handler. Maps known exception types to RFC 9457 problem responses and
/// everything else to an opaque 500 — stack traces and exception details never reach the client.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to answer.
            httpContext.Response.StatusCode = 499;
            return true;
        }

        var (statusCode, title, detail) = exception switch
        {
            BusinessRuleViolationException ex =>
                (StatusCodes.Status400BadRequest, "Business rule violated", ex.Message),
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The resource was modified by another request. Reload it and try again."),
            _ =>
                (StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred",
                    "The server encountered an internal error. Please try again later."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception handling {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation(
                "Request {Method} {Path} rejected with {StatusCode}: {Reason}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                detail);
        }

        httpContext.Response.StatusCode = statusCode;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
        });
    }
}
