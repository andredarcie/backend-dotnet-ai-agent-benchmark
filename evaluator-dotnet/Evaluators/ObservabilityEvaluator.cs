using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 9 — Observability (🟢 deterministic) — <b>INFORMATIONAL: weight 0, not ranked.</b>
///
/// Reported, never scored. At its old 4% weight this category could not separate two submissions, and its
/// two strongest signals were already decided elsewhere: /health is the executability gate (a service that
/// never answers it is capped at 1.5/5 regardless), and the PAN-never-logged rule is Security's. What is
/// left — structured JSON logs and a correlation id — is real practice worth SHOWING, but not a number
/// worth ranking on. The /metrics probe is gone with the requirement: the task no longer asks for it.</summary>
public sealed class ObservabilityEvaluator : CategoryEvaluatorBase
{
    public override int Number => 9;
    public override string Name => "Observability (informational)";
    public override double Weight => 0.00;   // informational — excluded from the weighted score
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        r.Metrics.Add(Bool("structured-logs",
            p.HasPackage("Serilog") || f.Invokes("AddJsonConsole", "UseSerilog", "AddSerilog"),
            "structured logging (JSON / Serilog)"));
        r.Metrics.Add(Bool("correlation",
            f.IdentifierContains("CorrelationId", "TraceId", "traceparent") || f.HasMemberAccess("Activity.Current"),
            "request correlation (trace/correlation id)"));

        // The static `health-endpoint` and `metrics-endpoint` metrics are gone: the first duplicated the
        // live probe below (and Resilience, and the gate); the second belonged to a /metrics requirement
        // the task has dropped.
        if (!string.IsNullOrEmpty(ctx.Options.BaseUrl))
        {
            var b = ctx.Options.BaseUrl.TrimEnd('/');
            var health = HttpProbe.Get(b + "/health");
            r.Metrics.Add(health.Reached && health.Status is >= 200 and < 400
                ? Pass("live-health", $"HTTP {health.Status}", "/health responds 2xx live")
                : Fail("live-health", health.Reached ? $"HTTP {health.Status}" : "unreachable", "/health responds 2xx live"));
        }

        r.Notes.Add("INFORMATIONAL: reported, but weight 0 — it does not move the score. /health is already enforced by the executability gate (capped at 1.5/5 if it never answers).");
        return Task.FromResult(r);
    }
}
