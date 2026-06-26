using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 11 — Performance &amp; Scalability (🟡 semi: needs a target SLO).
/// Roslyn detects async I/O, sync-over-async blocking, in-memory state and pagination. Load tests
/// (k6 / NBomber) need the app running (--deep).</summary>
public sealed class PerformanceEvaluator : CategoryEvaluatorBase
{
    public override int Number => 11;
    public override string Name => "Performance & Scalability";
    public override double Weight => 0.03;
    public override AutomationLevel Automation => AutomationLevel.SemiOracle;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var f = ctx.Facts;

        r.Metrics.Add(f.AsyncMethodCount > 0
            ? Pass("async-io", $"{f.AsyncMethodCount} async methods", "asynchronous/non-blocking I/O")
            : Fail("async-io", "0", "asynchronous/non-blocking I/O"));

        r.Metrics.Add(!f.HasBlockingCalls
            ? Pass("no-sync-over-async", "none", "no sync-over-async blocking (.Wait/.GetResult)")
            : Partial("no-sync-over-async", "found .Wait()/.GetResult()", "no sync-over-async blocking"));

        bool stateful = f.Invokes("AddSession") || f.StaticMutableFieldCount > 3;
        r.Metrics.Add(!stateful
            ? Pass("stateless", "no obvious in-memory state", "stateless API (horizontal scaling)")
            : Partial("stateless", $"{f.StaticMutableFieldCount} static mutable field(s)", "stateless API (horizontal scaling)", "review static mutable state/session"));

        r.Metrics.Add(Bool("pagination", f.IdentifierContains("pageSize", "pageNumber") || f.Invokes("Skip", "Take"),
            "pagination on collections (scaling proxy)", weight: 0.5));

        // Live concurrency smoke test: hammer a real endpoint with dozens of concurrent requests and
        // assert it stays up (no 5xx, no dropped connections). Runs whenever a base URL is available.
        var baseUrl = ctx.Options.BaseUrl;
        if (!string.IsNullOrEmpty(baseUrl))
        {
            var target = ctx.Contract?.RoutesBase ?? baseUrl.TrimEnd('/') + "/health";
            var burst = HttpProbe.Burst(target, total: 60, concurrency: 20);
            bool clean = burst.ServerErrors == 0 && burst.Failures == 0;
            r.Metrics.Add(clean
                ? Pass("concurrency", $"60 reqs @20 concurrent: 0 5xx, max {burst.MaxMs}ms", "survives concurrent load (no 5xx / hangs)")
                : Fail("concurrency", $"{burst.ServerErrors} 5xx, {burst.Failures} failed (of {burst.Sent})", "survives concurrent load (no 5xx / hangs)"));
        }

        // Heavier load test with explicit SLO thresholds (harness sets --base-url + BENCH_K6_SCRIPT).
        var script = Environment.GetEnvironmentVariable("BENCH_K6_SCRIPT");
        if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(script) && File.Exists(script))
        {
            RunTool(ctx, r, "k6", $"run --quiet -e BASE_URL={baseUrl} \"{script}\"", "load", "load test meets SLO thresholds (k6)",
                o => o.Success ? Pass("load", "thresholds met", "load test meets SLO thresholds (k6)", weight: 2)
                               : Fail("load", "thresholds breached", "load test meets SLO thresholds (k6)", weight: 2),
                weight: 2, timeoutMs: 180_000);
        }
        else r.Notes.Add("SEMI: the latency/throughput score needs a target SLO (oracle) and a load test against the running API (k6, set up by the docker-compose harness).");
        return Task.FromResult(r);
    }
}
