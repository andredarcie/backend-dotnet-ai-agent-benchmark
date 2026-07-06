using CreditCardApi.Application.Exceptions;
using CreditCardApi.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Presentation;

public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(httpContext, exception);
        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogInformation(exception, "Handled request exception");
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception) => exception switch
    {
        InvalidRequestException invalidRequest => new HttpValidationProblemDetails(invalidRequest.Errors)
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9457.html",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = httpContext.Request.Path
        },
        ResourceNotFoundException notFound => new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9457.html",
            Title = "Resource not found.",
            Detail = notFound.Message,
            Status = StatusCodes.Status404NotFound,
            Instance = httpContext.Request.Path
        },
        DomainRuleException domainRule => new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9457.html",
            Title = "Business rule violation.",
            Detail = domainRule.Message,
            Status = StatusCodes.Status400BadRequest,
            Instance = httpContext.Request.Path
        },
        DbUpdateConcurrencyException => new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9457.html",
            Title = "Concurrency conflict.",
            Detail = "The resource was modified by another request. Reload and try again.",
            Status = StatusCodes.Status409Conflict,
            Instance = httpContext.Request.Path
        },
        _ => new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9457.html",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = httpContext.Request.Path
        }
    };
}
