using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 1 — Functional Correctness &amp; Tests (🟡 oracle) — the heaviest category (20%).
///
/// The signal that decides it is the <b>live contract oracle</b>: the evaluator drives the real API
/// black-box against the running system and asserts the documented request→response contract. It carries
/// the bulk of the weight here, and it is the one signal the submission cannot write in its own favour.
///
/// The submission's own suite (formerly the separate "Tests" category) is folded in as the remainder: it
/// is a SELF-GRADED signal — a model can always write three trivial tests that pass — so it informs the
/// score without dominating it. What we ask of it is that it exists, is genuinely unit-only, and covers
/// the business rules.</summary>
public sealed class FunctionalCorrectnessEvaluator : CategoryEvaluatorBase
{
    public override int Number => 1;
    public override string Name => "Functional Correctness & Tests";
    public override double Weight => 0.20;
    public override AutomationLevel Automation => AutomationLevel.SemiOracle;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        // ---- the independent signal: the live contract oracle --------------------------------------
        // The real request->response contract, driven black-box against the running system (create card →
        // create transaction → read → list → 404s → the business rules). Not the submission's own tests,
        // and not its self-declared OpenAPI. These checks carry most of this category's weight.
        if (ctx.Contract is { Reachable: true } contract)
        {
            foreach (var check in contract.Checks.Where(c => c.Area == BackendEvaluator.Core.ContractArea.Functional))
                r.Metrics.Add(check.ToMetric());
        }
        else if (ctx.Contract is { Reachable: false })
        {
            r.Notes.Add("Live contract oracle could not resolve the credit-cards/transactions routes; correctness measured statically only.");
        }

        // ---- the submission's own suite (self-graded, so deliberately outweighed by the oracle) -----
        // Real unit tests exist: a test project that actually declares test cases (Roslyn sees the
        // [Fact]/[Theory]/[Test] attributes) — not merely a csproj referencing a framework. The old
        // `test-project`/`test-framework`/`coverage-tool` presence metrics are gone: they scored the same
        // fact three times, and "a package is referenced" is not an engineering signal.
        var testProjects = p.CsprojFiles.Where(x => Path.GetFileName(x).Contains("Test", StringComparison.OrdinalIgnoreCase)).ToList();
        r.Metrics.Add(Bool("unit-tests", testProjects.Count > 0 && f.UsesAttribute("Fact", "Theory", "Test"),
            "unit tests for the business rules"));

        // Unit-ONLY, as the task requires. Testcontainers needs a Docker daemon and boots a Postgres/Kafka
        // per run — explicitly forbidden. WebApplicationFactory is in-process but still an acceptance test
        // the task never asked for (that job belongs to the live oracle above), so it is partial credit,
        // not a pass: unrequested machinery is over-delivery, not a bonus.
        bool testcontainers = p.HasPackage("Testcontainers");
        bool webAppFactory = f.IdentifierEquals("WebApplicationFactory");
        r.Metrics.Add(testcontainers
            ? Fail("unit-only", "Testcontainers", "unit-only suite (no Docker/DB/broker needed)",
                   "the task forbids Testcontainers — the suite must run offline in seconds")
            : webAppFactory
                ? Partial("unit-only", "WebApplicationFactory", "unit-only suite (no integration/e2e tests)",
                          "acceptance testing is the live oracle's job, not the submission's")
                : Pass("unit-only", "unit tests only", "unit-only suite (no Docker/DB/broker needed)"));

        if (ctx.Options.Deep)
        {
            var outcome = ctx.RunDotnetTestOnce();   // ONE suite run feeds both the pass rate and coverage
            if (outcome == null)
            {
                r.MissingTools.Add("dotnet");
                r.Metrics.Add(Unknown("test-pass-rate", "100% of tests pass", "tool 'dotnet' not installed"));
                r.Metrics.Add(Unknown("coverage", "line coverage >=60%", "tool 'dotnet' not installed", 2));
            }
            else if (outcome.TimedOut)
            {
                r.Metrics.Add(Unknown("test-pass-rate", "100% of tests pass", "`dotnet test` timed out"));
                r.Metrics.Add(Unknown("coverage", "line coverage >=60%", "`dotnet test` timed out", 2));
            }
            else
            {
                AddPassRate(r, outcome);
                AddCoverage(r, p);
            }
        }
        else r.Notes.Add("Run with --deep to execute `dotnet test` (pass rate + Coverlet coverage).");

        if (ctx.Contract is null)
            r.Notes.Add("ORACLE: pass --base-url (the harness does) to run the live contract oracle — it carries most of this category's weight.");
        return Task.FromResult(r);
    }

    /// <summary>The submission's own pass rate — weight 1, i.e. deliberately small next to the oracle.</summary>
    private static void AddPassRate(CategoryResult r, ToolOutcome outcome)
    {
        var (passed, failed, _, parsed) = Heuristics.ParseDotnetTest(outcome.Combined);
        if (!parsed)
        {
            r.Metrics.Add(Unknown("test-pass-rate", "100% of tests pass", "could not parse `dotnet test` output"));
            return;
        }
        int total = passed + failed;
        double rate = total > 0 ? (double)passed / total : 0;
        r.Metrics.Add(Grade("test-pass-rate", rate, $"{passed}/{total} passed", "100% of tests pass"));
    }

    /// <summary>Real line coverage from Coverlet (XPlat Code Coverage), off the shared suite run.</summary>
    private static void AddCoverage(CategoryResult r, ProjectInspector p)
    {
        // Coverlet writes coverage.cobertura.xml under TestResults/ DURING the run — a directory the
        // inspector's startup snapshot excludes — so search the live filesystem, not the snapshot, or the
        // freshly-produced report would never be found. There is ONE report PER TEST PROJECT, so MERGE them
        // (union of covered lines): reading a single file silently understates the real coverage.
        List<string> reports;
        try { reports = Directory.EnumerateFiles(p.Root, "coverage.cobertura.xml", SearchOption.AllDirectories).ToList(); }
        catch { reports = new List<string>(); }

        if (reports.Count == 0)
        {
            r.Metrics.Add(Unknown("coverage", "line coverage >=60%", "no coverage.cobertura.xml produced", 2));
            return;
        }

        var merged = CoberturaCoverage.Merge(reports);
        if (!merged.Any)
        {
            r.Metrics.Add(Unknown("coverage", "line coverage >=60%", "coverage reports had no measurable lines", 2));
            return;
        }

        // Relaxed bar on purpose: reward a lean suite on the code that matters, not a number for its own
        // sake. Full credit at >=60%, half at >=35% — so there is no incentive to pad tests.
        //
        // The denominator is "the code that matters": CoberturaCoverage drops generated code (obj/, EF
        // Migrations) and the composition root (Program.cs), which no unit test can reach — and which the
        // task's OWN rules mandate (migrations) or forbid testing (no WebApplicationFactory). The excluded
        // count is reported so the number can be audited.
        double rate = merged.LineRate;
        string observed = $"{rate:P0} of {merged.Coverable} coverable lines ({merged.Reports} report(s))"
                          + (merged.Excluded > 0 ? $"; {merged.Excluded} generated/composition-root line(s) excluded" : "");
        r.Metrics.Add(Grade("coverage", rate >= 0.6 ? 1 : rate >= 0.35 ? 0.5 : 0,
            observed, "line coverage >=60% (on the code that matters)", weight: 2));
    }
}
