namespace BackendEvaluator.Core;

/// <summary>The local support tools cited in EVALUATION-CRITERIA.md: how to probe and how to install each.</summary>
public static class ToolCatalog
{
    public sealed record ToolInfo(string Probe, string Install);

    public static readonly IReadOnlyDictionary<string, ToolInfo> Tools = new Dictionary<string, ToolInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["dotnet"] = new("--version", "https://dotnet.microsoft.com/download"),
        ["docker"] = new("version", "https://docs.docker.com/get-docker/"),
        ["spectral"] = new("--version", "npm i -g @stoplight/spectral-cli"),
        ["semgrep"] = new("--version", "pip install semgrep"),
        ["trivy"] = new("--version", "https://aquasecurity.github.io/trivy/"),
        ["gitleaks"] = new("version", "https://github.com/gitleaks/gitleaks"),
        ["sqlfluff"] = new("--version", "pip install sqlfluff"),
        ["hadolint"] = new("--version", "https://github.com/hadolint/hadolint"),
        ["markdownlint"] = new("--version", "npm i -g markdownlint-cli"),
        ["lychee"] = new("--version", "https://github.com/lycheeverse/lychee"),
        ["k6"] = new("version", "https://k6.io/docs/get-started/installation/"),
        ["dotnet-stryker"] = new("--version", "dotnet tool install -g dotnet-stryker"),
        ["dotnet-outdated"] = new("--version", "dotnet tool install -g dotnet-outdated-tool"),
        ["reportgenerator"] = new("--help", "dotnet tool install -g dotnet-reportgenerator-globaltool"),
        ["oasdiff"] = new("--version", "https://github.com/oasdiff/oasdiff"),
        ["swagger-cli"] = new("--version", "npm i -g @apidevtools/swagger-cli"),
        ["schemacrawler"] = new("--version", "https://www.schemacrawler.com/"),
        ["toxiproxy-cli"] = new("--version", "https://github.com/Shopify/toxiproxy"),
    };

    public static string Probe(string tool) => Tools.TryGetValue(tool, out var t) ? t.Probe : "--version";
    public static string Install(string tool) => Tools.TryGetValue(tool, out var t) ? t.Install : "see the tool's documentation";

    public static bool IsAvailable(this ToolRunner tools, string tool) => tools.IsAvailable(tool, Probe(tool));
}
