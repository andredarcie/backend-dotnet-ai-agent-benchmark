using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 1 — Functional Suitability / Correctness (🟡 semi: needs an oracle suite).
/// Tools: dotnet test (run the suite), Stryker.NET (mutation). Roslyn detects the test setup.</summary>
public sealed class FunctionalCorrectnessEvaluator : CategoryEvaluatorBase
{
    public override int Number => 1;
    public override string Name => "Functional Suitability / Correctness";
    public override double Weight => 0.12;
    public override AutomationLevel Automation => AutomationLevel.SemiOracle;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        bool hasTestProj = p.CsprojFiles.Any(x => Path.GetFileName(x).Contains("Test", StringComparison.OrdinalIgnoreCase))
                           || p.HasPackage("xunit") || p.HasPackage("nunit") || p.HasPackage("MSTest");
        r.Metrics.Add(Bool("test-project", hasTestProj, "a test project exists (oracle suite)"));

        bool blackbox = f.IdentifierEquals("WebApplicationFactory") || p.HasPackage("Testcontainers");
        r.Metrics.Add(Bool("acceptance-blackbox", blackbox, "black-box acceptance tests (WebApplicationFactory / Testcontainers)"));

        // Mutation testing is OPTIONAL per the task ("skipping is fine"), so its absence must not penalize:
        // present -> a small bonus; absent -> Indeterminate (excluded from the score), not a Fail.
        bool mutationCfg = p.AnyFile("stryker-config.json", "stryker-config.yaml") || p.HasPackage("Stryker");
        r.Metrics.Add(mutationCfg
            ? Pass("mutation-config", "configured", "Stryker.NET mutation testing (optional bonus)", weight: 0.5)
            : Unknown("mutation-config", "Stryker.NET mutation testing (optional)", "optional per the task — absence is not penalized", weight: 0.5));

        // Live contract oracle: the real request->response contract, driven black-box against the
        // running system (Runner builds this once when --base-url is set). This is the independent
        // correctness signal — not the submission's own tests, nor its self-declared OpenAPI.
        if (ctx.Contract is { Reachable: true } contract)
        {
            foreach (var check in contract.Checks.Where(c => c.Area == BackendEvaluator.Core.ContractArea.Functional))
                r.Metrics.Add(check.ToMetric());
        }
        else if (ctx.Contract is { Reachable: false })
        {
            r.Notes.Add("Live contract oracle could not resolve the credit-cards/transactions routes; correctness measured statically only.");
        }

        // Fuzz the API against its own OpenAPI as a complementary check (catches schema drift).
        if (!string.IsNullOrEmpty(ctx.Options.BaseUrl))
        {
            var spec = ctx.Options.BaseUrl.TrimEnd('/') + (Environment.GetEnvironmentVariable("BENCH_OPENAPI_PATH") ?? "/swagger/v1/swagger.json");
            RunTool(ctx, r, "schemathesis", $"run \"{spec}\" --checks all --hypothesis-max-examples=20", "schemathesis",
                "API conforms to its OpenAPI contract (Schemathesis)",
                o => o.Success ? Pass("schemathesis", "conforms", "API conforms to its OpenAPI contract (Schemathesis)", weight: 1)
                               : Fail("schemathesis", "violations found", "API conforms to its OpenAPI contract (Schemathesis)", weight: 1),
                weight: 1, timeoutMs: 240_000);
        }

        // Real tool: run the shared suite once (see EvaluationContext.RunDotnetTestOnce, collected with
        // coverage and reused by Tests #9) and read the pass rate from its Passed/Failed summary.
        if (ctx.Options.Deep)
        {
            var outcome = ctx.RunDotnetTestOnce();
            if (outcome == null)
            {
                r.MissingTools.Add("dotnet");
                r.Metrics.Add(Unknown("test-pass-rate", "100% of tests pass", "tool 'dotnet' not installed", 2));
            }
            else if (outcome.TimedOut)
            {
                r.Metrics.Add(Unknown("test-pass-rate", "100% of tests pass", "`dotnet test` timed out", 2));
            }
            else
            {
                var (passed, failed, _, parsed) = Heuristics.ParseDotnetTest(outcome.Combined);
                if (!parsed)
                    r.Metrics.Add(Unknown("test-pass-rate", "100% of tests pass", "could not parse `dotnet test` output", 2));
                else
                {
                    int total = passed + failed;
                    double rate = total > 0 ? (double)passed / total : 0;
                    r.Metrics.Add(Grade("test-pass-rate", rate, $"{passed}/{total} passed", "100% of tests pass", weight: 2));
                }
            }
        }
        else r.Notes.Add("Run with --deep to execute `dotnet test` and measure the real pass rate.");

        if (ctx.Contract is null)
            r.Notes.Add("SEMI: pass --base-url (the harness does) to run the live contract oracle that measures real per-endpoint correctness.");
        return Task.FromResult(r);
    }
}
