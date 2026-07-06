using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Presentation;

public static class InvalidModelStateResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problemDetails = new HttpValidationProblemDetails(errors)
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9457.html",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new JsonResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentType = "application/problem+json"
        };
    }
}


