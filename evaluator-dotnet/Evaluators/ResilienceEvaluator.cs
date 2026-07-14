using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 8 — Resilience &amp; Error Handling (🟢 deterministic).
/// Roslyn detects Polly policies, health checks, the global exception handler, graceful shutdown and
/// timeouts/cancellation; the live oracle proves a malformed request leaks no stack trace.</summary>
public sealed class ResilienceEvaluator : CategoryEvaluatorBase
{
    public override int Number => 8;
    public override string Name => "Resilience & Error Handling";
    public override double Weight => 0.04;
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

        // No `health-checks` metric here. Health was being scored THREE times — statically in this
        // category, statically again in Observability, and by the live /health probe — while the
        // executability gate already caps any submission whose /health never answers at 1.5/5. It was the
        // most over-counted signal in the rubric and the one least able to change an outcome.

        r.Metrics.Add(Bool("global-error-handling",
            f.IdentifierEquals("IExceptionHandler") || f.Invokes("UseExceptionHandler", "UseProblemDetails", "AddProblemDetails"),
            "global exception handling (no stack-trace leak)"));

        // `IHostedService` counts alongside `BackgroundService`: it is THE hosted-service abstraction, and
        // BackgroundService is merely a convenience base class that implements it. A submission that
        // implements IHostedService directly (with its own StartAsync/StopAsync) participates in the host's
        // shutdown lifecycle exactly the same way — failing it for not picking the subclass was scoring the
        // idiom, not the behaviour. Serilog's CloseAndFlush on exit is likewise real shutdown handling.
        r.Metrics.Add(Bool("graceful-shutdown",
            f.IdentifierEquals("IHostApplicationLifetime", "IHostedService", "BackgroundService")
            || f.IdentifierContains("ApplicationStopping")
            || f.Invokes("StopAsync", "CloseAndFlush", "CloseAndFlushAsync"),
            "graceful shutdown / hosted services", weight: 0.5));

        r.Metrics.Add(Bool("timeouts",
            f.Invokes("AddRequestTimeouts") || f.IdentifierEquals("CommandTimeout", "CancellationToken"),
            "timeouts / cancellation propagated", weight: 0.5));

        // Live: a malformed request is handled without leaking a stack trace (global handler at work).
        if (ctx.Contract is { Reachable: true } contract)
            foreach (var ch in contract.Checks.Where(c => c.Area == BackendEvaluator.Core.ContractArea.Resilience))
                r.Metrics.Add(ch.ToMetric());

        return Task.FromResult(r);
    }
}
