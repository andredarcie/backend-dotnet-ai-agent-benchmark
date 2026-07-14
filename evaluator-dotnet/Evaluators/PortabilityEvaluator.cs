using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 10 — Portability &amp; Deploy (🟢 deterministic) — <b>INFORMATIONAL: weight 0.</b>
///
/// Reported, never scored — because the part of it that matters is not a checkbox here, it is the
/// executability gate: the harness boots the submission's OWN docker-compose, and a project that does not
/// come up is capped at 1.0–1.5/5 no matter how clean its Dockerfile lints. Ranking "a Dockerfile exists"
/// at 2% on top of that was double-counting a fact the run already decided.
/// Tools: hadolint (Dockerfile lint); file checks for compose, env config, pinning and a non-root USER.</summary>
public sealed class PortabilityEvaluator : CategoryEvaluatorBase
{
    public override int Number => 10;
    public override string Name => "Portability & Deploy (informational)";
    public override double Weight => 0.00;   // informational — the executability gate is the real signal
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        var dockerfile = p.FindByName("Dockerfile").FirstOrDefault();
        r.Metrics.Add(Bool("dockerfile", dockerfile != null, "Dockerfile present"));

        bool compose = p.FindByNamePattern(@"^docker-compose(\.\w+)?\.ya?ml$").Any() || p.AnyFile("compose.yaml", "compose.yml");
        r.Metrics.Add(Bool("compose", compose, "docker-compose for dependencies"));

        bool envConfig = f.Invokes("GetEnvironmentVariable") || f.IdentifierEquals("IConfiguration") || f.HasMemberAccess("builder.Configuration");
        r.Metrics.Add(Bool("env-config", envConfig, "externalized config (env vars, 12-Factor III/IV)"));

        // Central Package Management (Directory.Packages.props with ManagePackageVersionsCentrally) pins
        // every version centrally — the task explicitly accepts it alongside a lock file / global.json.
        bool cpm = false;
        var cpmFile = p.FindByName("Directory.Packages.props").FirstOrDefault();
        if (cpmFile != null)
        {
            try
            {
                cpm = System.Xml.Linq.XDocument.Load(cpmFile).Descendants()
                    .Any(e => e.Name.LocalName == "ManagePackageVersionsCentrally"
                              && string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
            }
            catch { /* unreadable props — fall back to lock-file detection */ }
        }
        r.Metrics.Add(Bool("pinning", p.AnyFile("packages.lock.json") || p.AnyFile("global.json") || cpm,
            "pinned dependencies (lock file / global.json / Central Package Management)", weight: 0.5));

        // No `ci` metric, and the task no longer asks for a CI workflow: nothing in this benchmark ever
        // RUNS it, so it scored the existence of a YAML file — the definition of ceremony. What CI would
        // have proven (it builds, its tests pass, it lints) the evaluator already does itself, for real.

        bool nonRoot = false;
        if (dockerfile != null)
        {
            try { nonRoot = System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(dockerfile), @"^\s*USER\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline); }
            catch { }
        }
        r.Metrics.Add(Bool("non-root", nonRoot, "container runs as non-root", weight: 0.5));

        // Real tool: Dockerfile lint.
        if (dockerfile != null)
            RunTool(ctx, r, "hadolint", $"\"{dockerfile}\"", "hadolint", "Dockerfile with no violations (hadolint)",
                o => o.Success ? Pass("hadolint", "clean", "Dockerfile with no violations (hadolint)", weight: 0.5)
                               : Partial("hadolint", "violations", "Dockerfile with no violations (hadolint)", weight: 0.5), weight: 0.5);

        r.Notes.Add("INFORMATIONAL: reported, but weight 0 — whether the project actually deploys is decided by the executability gate (the harness boots its own compose), not by this checklist.");
        return Task.FromResult(r);
    }
}
