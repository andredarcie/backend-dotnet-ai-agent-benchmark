using System.Diagnostics;
using CreditCardApi.Infrastructure.Observability;

namespace CreditCardApi.Api.Middleware;

/// <summary>
/// Establishes a correlation id for every request: honors an incoming <c>X-Correlation-Id</c>
/// header (or falls back to the trace id), echoes it on the response, stamps it on the current
/// activity's baggage (from where the outbox propagates it into Kafka headers), and pushes it
/// into the logging scope so every log line of the request carries it.
/// </summary>
internal sealed class CorrelationIdMiddleware
{
    private const int MaxIncomingLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[Correlation.HeaderName] = correlationId;
        Activity.Current?.SetBaggage(Correlation.BaggageKey, correlationId);
        Activity.Current?.SetTag(Correlation.BaggageKey, correlationId);

        // OnStarting survives the response reset performed by the exception handler middleware,
        // so the header is present on error responses too.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Correlation.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[Correlation.HeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(incoming) && incoming.Length <= MaxIncomingLength)
        {
            return incoming;
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}
