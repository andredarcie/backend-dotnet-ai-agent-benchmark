using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 12 — Portability, Configuration &amp; Deploy (🟢 full-auto).
/// Tools: hadolint (Dockerfile lint), dotnet-outdated (dependency freshness). File/Roslyn checks for
/// compose, env-based config, dependency pinning, CI and non-root container.</summary>
public sealed class PortabilityEvaluator : CategoryEvaluatorBase
{
    public override int Number => 12;
    public override string Name => "Portability, Configuration & Deploy";
    public override double Weight => 0.02;
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
        r.Metrics.Add(Bool("ci", p.AnyPathContains(".github/workflows/") || p.AnyFile(".gitlab-ci.yml", "azure-pipelines.yml"), "CI pipeline present", weight: 0.5));

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

        if (ctx.Options.Deep)
            RunTool(ctx, r, "dotnet-outdated", $"\"{p.Root}\"", "outdated", "dependencies reasonably up to date",
                o => o.Success ? Pass("outdated", "checked", "dependencies reasonably up to date", weight: 0.5)
                               : Partial("outdated", "outdated packages found", "dependencies reasonably up to date", weight: 0.5), weight: 0.5);

        return Task.FromResult(r);
    }
}
