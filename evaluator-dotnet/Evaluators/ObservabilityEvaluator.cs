using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 10 — Observability (🟢 deterministic, enabler).
/// Roslyn/packages detect OpenTelemetry, structured logging, the metrics endpoint, correlation and
/// health wiring. Live scraping (Prometheus / OTel Collector) needs the app running (--deep).</summary>
public sealed class ObservabilityEvaluator : CategoryEvaluatorBase
{
    public override int Number => 10;
    public override string Name => "Observability (enabler)";
    public override double Weight => 0.04;
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        r.Metrics.Add(Bool("otel", p.HasPackage("OpenTelemetry") || f.UsesNamespace("OpenTelemetry"),
            "OpenTelemetry (traces/metrics/logs)"));
        r.Metrics.Add(Bool("structured-logs",
            p.HasPackage("Serilog") || f.Invokes("AddJsonConsole", "UseSerilog", "AddSerilog"),
            "structured logging (JSON / Serilog)"));
        r.Metrics.Add(Bool("metrics-endpoint",
            f.Invokes("AddPrometheusExporter", "MapPrometheusScrapingEndpoint") || f.UsesGeneric("Meter") || f.IdentifierEquals("Meter"),
            "metrics exposed (Prometheus / Meter)", weight: 0.5));
        r.Metrics.Add(Bool("correlation",
            f.IdentifierContains("CorrelationId", "TraceId", "traceparent") || f.HasMemberAccess("Activity.Current"),
            "request correlation (trace/correlation id)", weight: 0.5));
        r.Metrics.Add(Bool("health-endpoint", f.Invokes("MapHealthChecks", "AddHealthChecks"),
            "health/diagnostics endpoint", weight: 0.5));

        // Live probes against the running system (harness sets --base-url).
        if (!string.IsNullOrEmpty(ctx.Options.BaseUrl))
        {
            var b = ctx.Options.BaseUrl.TrimEnd('/');
            var health = HttpProbe.Get(b + "/health");
            r.Metrics.Add(health.Reached && health.Status is >= 200 and < 400
                ? Pass("live-health", $"HTTP {health.Status}", "/health responds 2xx live")
                : Fail("live-health", health.Reached ? $"HTTP {health.Status}" : "unreachable", "/health responds 2xx live"));

            var metrics = HttpProbe.Get(b + "/metrics");
            r.Metrics.Add(metrics.Reached && metrics.Status is >= 200 and < 400
                ? Pass("live-metrics", $"HTTP {metrics.Status}", "/metrics responds live", weight: 0.5)
                : Fail("live-metrics", metrics.Reached ? $"HTTP {metrics.Status}" : "unreachable", "/metrics responds live", weight: 0.5));
        }

        return Task.FromResult(r);
    }
}
