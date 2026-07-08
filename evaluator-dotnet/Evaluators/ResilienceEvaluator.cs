using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 8 — Resilience &amp; Error Handling (🟢 deterministic).
/// Roslyn detects Polly policies, health checks, the global exception handler, graceful shutdown and
/// timeouts/cancellation. Real fault injection (Toxiproxy) needs the app running (--deep).</summary>
public sealed class ResilienceEvaluator : CategoryEvaluatorBase
{
    public override int Number => 8;
    public override string Name => "Resilience & Error Handling";
    public override double Weight => 0.08;
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        r.Metrics.Add(Bool("resilience-policies",
            p.HasPackage("Polly") || p.HasPackage("Microsoft.Extensions.Http.Resilience")
            || f.Invokes("AddResilienceHandler", "WaitAndRetry", "AddPolicyHandler", "AddStandardResilienceHandler"),
            "retry/timeout/circuit-breaker policies (Polly)"));

        r.Metrics.Add(Bool("health-checks", f.Invokes("AddHealthChecks", "MapHealthChecks", "UseHealthChecks"),
            "health checks (liveness/readiness)"));

        r.Metrics.Add(Bool("global-error-handling",
            f.IdentifierEquals("IExceptionHandler") || f.Invokes("UseExceptionHandler", "UseProblemDetails", "AddProblemDetails"),
            "global exception handling (no stack-trace leak)"));

        r.Metrics.Add(Bool("graceful-shutdown",
            f.IdentifierEquals("IHostApplicationLifetime", "BackgroundService") || f.IdentifierContains("ApplicationStopping") || f.Invokes("StopAsync"),
            "graceful shutdown / hosted services", weight: 0.5));

        r.Metrics.Add(Bool("timeouts",
            f.Invokes("AddRequestTimeouts") || f.IdentifierEquals("CommandTimeout", "CancellationToken"),
            "timeouts / cancellation propagated", weight: 0.5));

        // Live: a malformed request is handled without leaking a stack trace (global handler at work).
        if (ctx.Contract is { Reachable: true } contract)
            foreach (var ch in contract.Checks.Where(c => c.Area == BackendEvaluator.Core.ContractArea.Resilience))
                r.Metrics.Add(ch.ToMetric());

        if (ctx.Options.Deep && !ctx.Tools.IsAvailable("toxiproxy-cli")) r.MissingTools.Add("toxiproxy-cli");
        r.Notes.Add("Real fault injection (Toxiproxy) and recovery measurement need --deep + a running container.");
        return Task.FromResult(r);
    }
}
