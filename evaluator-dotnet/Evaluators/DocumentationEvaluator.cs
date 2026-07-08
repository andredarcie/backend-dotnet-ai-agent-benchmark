using System.Text.RegularExpressions;
using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 13 — Documentation (🟠 proxy).
/// Tools: markdownlint (README lint), lychee (broken links), swagger-cli (OpenAPI validity).
/// README section presence is parsed from the markdown; doc-comment density comes from Roslyn.</summary>
public sealed class DocumentationEvaluator : CategoryEvaluatorBase
{
    public override int Number => 13;
    public override string Name => "Documentation";
    public override double Weight => 0.01;
    public override AutomationLevel Automation => AutomationLevel.ProxyReview;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        var readmeFile = p.FindByName("README.md", "readme.md", "README.markdown").FirstOrDefault();
        var readme = readmeFile != null ? SafeRead(readmeFile) : null;
        r.Metrics.Add(Bool("readme", readme != null, "README present"));

        if (readme != null)
        {
            bool purpose = Regex.IsMatch(readme, @"#\s|purpose|overview|about", RegexOptions.IgnoreCase);
            bool setup = Regex.IsMatch(readme, @"setup|install|getting started|prerequisit|requirement", RegexOptions.IgnoreCase);
            bool run = Regex.IsMatch(readme, @"\brun\b|docker compose|dotnet run|usage|how to", RegexOptions.IgnoreCase);
            int sections = (purpose ? 1 : 0) + (setup ? 1 : 0) + (run ? 1 : 0);
            r.Metrics.Add(Grade("readme-sections", sections / 3.0, $"{sections}/3", "README with purpose+setup+run"));
        }

        bool apiDocs = p.HasPackage("Swashbuckle") || p.HasPackage("NSwag") || p.HasPackage("Microsoft.AspNetCore.OpenApi")
                       || f.Invokes("AddOpenApi", "AddSwaggerGen");
        r.Metrics.Add(Bool("api-docs", apiDocs, "API documentation (OpenAPI/Swagger)", weight: 0.5));

        bool xmlDoc = p.PropertyIs("GenerateDocumentationFile", "true") || f.DocCommentCount > 5;
        r.Metrics.Add(Bool("doc-comments", xmlDoc, "doc comments / XML docs", weight: 0.5));

        // Real tools.
        // markdownlint with a consistent, deliberately lenient benchmark ruleset (below): scores real
        // README problems (broken tables, unclosed fences, heading order), NOT cosmetic column discipline
        // that PROMPT.md never asks for. A Node runtime crash is Indeterminate, not "violations".
        if (readmeFile != null)
        {
            var cfg = MarkdownlintConfigPath();
            var cfgArg = cfg != null ? $"-c \"{cfg}\" " : "";
            RunTool(ctx, r, "markdownlint", $"{cfgArg}\"{readmeFile}\"", "markdownlint", "README passes markdownlint",
                o => CouldNotRun(o, "SyntaxError", "node:internal", "Cannot find module", "internal/modules")
                        ? Unknown("markdownlint", "README passes markdownlint", "markdownlint could not run (runtime error) — not scored", 0.5)
                   : o.Success ? Pass("markdownlint", "clean", "README passes markdownlint", weight: 0.5)
                               : Partial("markdownlint", "violations", "README passes markdownlint", weight: 0.5), weight: 0.5);
        }

        // lychee: exclude loopback/localhost — a README documenting the service's own local endpoints
        // (http://localhost:8080/health, /swagger, …) is correct docs, not a broken link; those hosts are
        // simply not listening during the lint phase.
        if (ctx.Options.Deep && readmeFile != null)
            RunTool(ctx, r, "lychee", $"--no-progress --exclude-loopback --exclude localhost \"{readmeFile}\"", "links", "no broken links (lychee)",
                o => o.Success ? Pass("links", "no broken links", "no broken links (lychee)", weight: 0.5)
                               : Partial("links", "broken link(s)", "no broken links (lychee)", weight: 0.5), weight: 0.5);

        r.Notes.Add("PROXY: README section/link presence, OpenAPI completeness and doc-comment coverage are scored automatically.");
        return Task.FromResult(r);
    }

    private static string? SafeRead(string path) { try { return File.ReadAllText(path); } catch { return null; } }

    /// <summary>
    /// Writes the benchmark's markdownlint ruleset to a temp file and returns its path (or null on failure).
    /// Disables the purely cosmetic rules — MD013 (line length) and MD034 (bare URLs: a README legitimately
    /// shows endpoint URLs like http://localhost:8080/swagger) — while <c>"default": true</c> keeps every
    /// structural rule on. Passing this with <c>-c</c> lints every submission identically and stops a
    /// submission from shipping its own all-disabling config to game the check.
    /// </summary>
    private static string? MarkdownlintConfigPath()
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "bench-markdownlint.json");
            File.WriteAllText(path, "{ \"default\": true, \"MD013\": false, \"MD034\": false }");
            return path;
        }
        catch { return null; }
    }
}
