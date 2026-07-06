using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditCardApi.Api.HealthChecks;

/// <summary>JSON response writers for the health endpoints.</summary>
internal static class HealthResponseWriter
{
    /// <summary>Writes the contract-mandated minimal body, e.g. <c>{"status":"healthy"}</c>.</summary>
    public static Task WriteMinimalAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";
        return context.Response.WriteAsync($"{{\"status\":\"{status}\"}}", context.RequestAborted);
    }

    /// <summary>Writes the overall status plus one entry per registered check.</summary>
    public static Task WriteDetailedAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = JsonSerializer.Serialize(new
        {
            status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy",
            totalDurationMs = (long)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString().ToLowerInvariant(),
                    description = entry.Value.Description,
                    durationMs = (long)entry.Value.Duration.TotalMilliseconds,
                }),
        });
        return context.Response.WriteAsync(payload, context.RequestAborted);
    }
}
