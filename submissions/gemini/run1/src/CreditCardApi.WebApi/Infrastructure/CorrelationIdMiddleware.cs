using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.WebApi.Infrastructure;

/// <summary>
/// Middleware to propagate a correlation ID end-to-end for request tracking and observability.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderKey = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        // 1. Get or generate correlation ID
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationIdValue))
        {
            correlationIdValue = Guid.NewGuid().ToString();
        }

        var correlationId = correlationIdValue.ToString();

        // 2. Add to response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderKey] = correlationId;
            return Task.CompletedTask;
        });

        // 3. Push CorrelationId to logger scope so all logs in this request share it
        var logScope = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = context.TraceIdentifier
        };

        using (logger.BeginScope(logScope))
        {
            await _next(context);
        }
    }
}
