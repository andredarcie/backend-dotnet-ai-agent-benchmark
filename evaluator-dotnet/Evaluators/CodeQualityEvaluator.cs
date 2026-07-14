using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 3 — Code Quality (🟢 deterministic).
/// Tools: dotnet format (--verify-no-changes) and dotnet build (analyzer warnings). Roslyn detects empty
/// catches, TODO/FIXME comments, micro-optimization markers and sync-over-async blocking.
///
/// Async/blocking I/O lives here, not in a category of its own: the former "Performance &amp; Scalability"
/// category was 3% of the score and, once the load test was dropped, held nothing but these two Roslyn
/// facts plus two metrics that could never fail (`stateless`) or were already asserted live (`pagination`,
/// which the contract oracle proves for real). Sync-over-async in an ASP.NET Core service is a code
/// defect, so it is scored as one.</summary>
public sealed class CodeQualityEvaluator : CategoryEvaluatorBase
{
    public override int Number => 3;
    public override string Name => "Code Quality";
    public override double Weight => 0.10;
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        r.Metrics.Add(f.EmptyCatchCount == 0
            ? Pass("no-empty-catch", "0", "no empty catch (no swallowed exceptions)")
            : Fail("no-empty-catch", f.EmptyCatchCount.ToString(), "no empty catch (no swallowed exceptions)"));

        r.Metrics.Add(Grade("no-todos", f.TodoCommentCount == 0 ? 1 : f.TodoCommentCount <= 3 ? 0.5 : 0,
            f.TodoCommentCount.ToString(), "no pending TODO/FIXME/HACK"));

        bool analyzers = p.PropertyIs("TreatWarningsAsErrors", "true") || p.PropertyIs("EnableNETAnalyzers", "true") || p.AnyFile(".editorconfig");
        r.Metrics.Add(Bool("analyzers-enabled", analyzers, "analyzers/.editorconfig enabled"));

        // Idiomatic async I/O (was category 11). A blocking call on a request path (.Result / .Wait() /
        // .GetAwaiter().GetResult()) is a thread-pool starvation bug in an ASP.NET Core service — a Fail,
        // not the half-credit it used to earn.
        r.Metrics.Add(Bool("async-io", f.AsyncMethodCount > 0,
            $"asynchronous, non-blocking I/O ({f.AsyncMethodCount} async methods)"));
        r.Metrics.Add(Bool("no-sync-over-async", !f.HasBlockingCalls,
            "no sync-over-async blocking (.Result/.Wait()/.GetResult())"));

        if (f.HasUnsafeOrStackalloc)
            r.Notes.Add("FLAG (optional review): unsafe/stackalloc present - micro-optimizations are acceptable only with a benchmark proving the gain.");

        // Real tools.
        if (ctx.Options.Deep)
        {
            string formatArgs = ctx.SolutionPath != null
                ? $"format \"{ctx.SolutionPath}\" --verify-no-changes --verbosity quiet"
                : "format --verify-no-changes --verbosity quiet";
            RunTool(ctx, r, "dotnet", formatArgs, "format", "code is formatted (dotnet format)",
                o => o.Success ? Pass("format", "no changes", "code is formatted (dotnet format)")
                               : Fail("format", "formatting diverges", "code is formatted (dotnet format)"),
                timeoutMs: 300_000);

            // Warning count comes from Runner's single Release build gate (avoids a second full build here).
            r.Metrics.Add(ctx.BuildWarnings is { } warns
                ? Grade("build-warnings", warns == 0 ? 1 : warns <= 10 ? 0.5 : 0, $"{warns} warning(s)", "0 build warnings")
                : Unknown("build-warnings", "0 build warnings", "build summary unavailable"));
        }
        else r.Notes.Add("Run with --deep to execute `dotnet format` and `dotnet build` (analyzer warnings).");

        return Task.FromResult(r);
    }
}
