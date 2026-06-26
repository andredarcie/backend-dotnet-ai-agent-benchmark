using System.Text.RegularExpressions;
using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 13 — Documentation (🟠 proxy + review).
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
        if (readmeFile != null)
            RunTool(ctx, r, "markdownlint", $"\"{readmeFile}\"", "markdownlint", "README passes markdownlint",
                o => o.Success ? Pass("markdownlint", "clean", "README passes markdownlint", weight: 0.5)
                               : Partial("markdownlint", "violations", "README passes markdownlint", weight: 0.5), weight: 0.5);

        if (ctx.Options.Deep && readmeFile != null)
            RunTool(ctx, r, "lychee", $"--no-progress \"{readmeFile}\"", "links", "no broken links (lychee)",
                o => o.Success ? Pass("links", "no broken links", "no broken links (lychee)", weight: 0.5)
                               : Partial("links", "broken link(s)", "no broken links (lychee)", weight: 0.5), weight: 0.5);

        r.Notes.Add("PROXY: section/link presence is automatic, but the QUALITY of the prose needs human review.");
        return Task.FromResult(r);
    }

    private static string? SafeRead(string path) { try { return File.ReadAllText(path); } catch { return null; } }
}
