namespace BackendEvaluator.Core;

/// <summary>
/// The local support tools the evaluator actually invokes: how to probe and how to install each.
///
/// The set is deliberately small, so a grading run is cheap and reproducible (same source ⇒ same score).
/// Tools whose rule set drifts over time (Semgrep `--config auto`, Trivy's CVE database, OWASP ZAP,
/// Schemathesis, dotnet-outdated, lychee) were retired: they dominated the wall-clock, and a score that
/// changes because a remote rule set changed is not a benchmark score.
///
/// One honest caveat: `dotnet` still reaches nuget.org for `restore` and for the vulnerability audit
/// behind `list package --vulnerable`, whose data moves as CVEs are disclosed. That single metric is
/// therefore time-dependent (and is reported Indeterminate when no source is reachable, rather than
/// silently passing — see SecurityEvaluator). Roslyn, gitleaks and hadolint are fully offline.
/// </summary>
public static class ToolCatalog
{
    public sealed record ToolInfo(string Probe, string Install);

    public static readonly IReadOnlyDictionary<string, ToolInfo> Tools = new Dictionary<string, ToolInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["dotnet"] = new("--version", "https://dotnet.microsoft.com/download"),
        ["docker"] = new("version", "https://docs.docker.com/get-docker/"),
        ["gitleaks"] = new("version", "https://github.com/gitleaks/gitleaks"),
        ["hadolint"] = new("--version", "https://github.com/hadolint/hadolint"),
    };

    public static string Probe(string tool) => Tools.TryGetValue(tool, out var t) ? t.Probe : "--version";
    public static string Install(string tool) => Tools.TryGetValue(tool, out var t) ? t.Install : "see the tool's documentation";

    public static bool IsAvailable(this ToolRunner tools, string tool) => tools.IsAvailable(tool, Probe(tool));
}
