using Serilog.Context;

namespace CreditCardApi.Api.Middleware;

/// <summary>
/// Propagates a correlation id end to end: reuses an inbound X-Correlation-Id if the caller sent
/// one, otherwise mints one, echoes it on the response, and pushes it onto every log line written
/// while handling this request.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
