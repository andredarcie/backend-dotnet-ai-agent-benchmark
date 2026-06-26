using System.Globalization;
using System.Text.RegularExpressions;
using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 9 — Tests (🟢 full-auto, enabler).
/// Tools: dotnet test + Coverlet (XPlat Code Coverage) for real line coverage; Stryker.NET for mutation.
/// Roslyn/packages detect the framework and the pyramid shape.</summary>
public sealed class TestsEvaluator : CategoryEvaluatorBase
{
    public override int Number => 9;
    public override string Name => "Tests (enabler)";
    public override double Weight => 0.08;
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        // Presence of a self-authored suite is a weak signal (a model can write trivial tests), so it
        // carries low weight; the real weight is on measured coverage (deep) and the independent live
        // contract oracle (category 1). This reduces the self-grading effect.
        var testProjects = p.CsprojFiles.Where(x => Path.GetFileName(x).Contains("Test", StringComparison.OrdinalIgnoreCase)).ToList();
        bool framework = testProjects.Count > 0 || p.HasPackage("xunit") || p.HasPackage("nunit") || p.HasPackage("MSTest");
        r.Metrics.Add(Bool("test-framework", framework, "test project(s) with a framework", weight: 0.5));

        bool unit = p.AnyDir("UnitTests") || p.CsprojFiles.Any(x => Path.GetFileName(x).Contains("Unit", StringComparison.OrdinalIgnoreCase));
        bool integration = p.AnyDir("IntegrationTests") || p.HasPackage("Testcontainers") || f.IdentifierEquals("WebApplicationFactory");
        r.Metrics.Add(unit || integration
            ? Pass("pyramid", $"unit={unit}, integration={integration}", "pyramid: unit + integration")
            : Partial("pyramid", "types not identified", "pyramid: unit + integration"));

        r.Metrics.Add(Bool("coverage-tool", p.HasPackage("coverlet"), "coverage tool (Coverlet)", weight: 0.5));
        // Mutation testing is OPTIONAL per the task — present is a bonus, absent is Indeterminate (not a Fail).
        bool mutation = p.AnyFile("stryker-config.json", "stryker-config.yaml") || p.HasPackage("Stryker");
        r.Metrics.Add(mutation
            ? Pass("mutation-tool", "configured", "mutation testing (Stryker.NET, optional bonus)", weight: 0.5)
            : Unknown("mutation-tool", "mutation testing (Stryker.NET, optional)", "optional per the task — absence is not penalized", weight: 0.5));

        // Real tool: measure coverage from the shared `dotnet test` run (see EvaluationContext.RunDotnetTestOnce,
        // which collects XPlat Code Coverage). Functional (#1) and Tests (#9) thus share a single suite run.
        if (ctx.Options.Deep)
        {
            var outcome = ctx.RunDotnetTestOnce();
            if (outcome == null)
            {
                r.MissingTools.Add("dotnet");
                r.Metrics.Add(Unknown("coverage", "line coverage >=80%", "tool 'dotnet' not installed", 2));
            }
            else if (outcome.TimedOut)
            {
                r.Metrics.Add(Unknown("coverage", "line coverage >=80%", "`dotnet test` timed out", 2));
            }
            else
            {
                // Coverlet writes coverage.cobertura.xml under TestResults/ DURING the run — a directory the
                // inspector's startup snapshot excludes (IgnoreDirs) — so search the live filesystem here,
                // not the snapshot, or the freshly-produced report would never be found.
                string? cobertura;
                try
                {
                    cobertura = Directory.EnumerateFiles(p.Root, "coverage.cobertura.xml", SearchOption.AllDirectories)
                        .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                }
                catch { cobertura = null; }
                if (cobertura == null)
                    r.Metrics.Add(Unknown("coverage", "line coverage >=80%", "no coverage.cobertura.xml produced", 2));
                else
                {
                    MetricResult metric = Unknown("coverage", "line coverage >=80%", "could not read cobertura line-rate", 2);
                    try
                    {
                        var m = Regex.Match(File.ReadAllText(cobertura), "line-rate=\"([0-9.]+)\"");
                        if (m.Success && double.TryParse(m.Groups[1].Value, CultureInfo.InvariantCulture, out var rate))
                            metric = Grade("coverage", rate >= 0.8 ? 1 : rate >= 0.5 ? 0.5 : 0, $"{rate:P0}", "line coverage >=80%", weight: 2);
                    }
                    catch { }
                    r.Metrics.Add(metric);
                }
            }
        }
        else r.Notes.Add("Run with --deep to measure real coverage (Coverlet) and, optionally, mutation score (Stryker.NET).");

        return Task.FromResult(r);
    }
}
