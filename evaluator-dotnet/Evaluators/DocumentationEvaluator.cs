using System.Text.RegularExpressions;
using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 11 — Documentation (🟠 proxy) — <b>INFORMATIONAL: weight 0, not ranked.</b>
///
/// Reported, never scored. At its old 1% weight, the gap between a perfect README and no README at all
/// moved the final score by 0.05 — less than the run-to-run noise of the same model on the same prompt.
/// It was a number that looked like a measurement and could not act like one. What genuinely matters
/// about the contract — that the served OpenAPI actually describes the endpoints — is asserted LIVE by
/// category 4 (`openapi-populated`), where it counts.</summary>
public sealed class DocumentationEvaluator : CategoryEvaluatorBase
{
    public override int Number => 11;
    public override string Name => "Documentation (informational)";
    public override double Weight => 0.00;   // informational — excluded from the weighted score
    public override AutomationLevel Automation => AutomationLevel.ProxyReview;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;

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

        // No `api-docs` metric: it was the same package/invocation predicate as category 4's OpenAPI check,
        // scoring one fact twice — and the live `openapi-populated` probe there is the stronger form of it.
        // No `doc-comments` metric either, and the task no longer asks for XML docs: `///` density measures
        // typing, not engineering, and it was trivially gamed by a model that comments every property.

        r.Notes.Add("INFORMATIONAL: reported, but weight 0 — it does not move the score. That the OpenAPI document really describes the endpoints is asserted live by category 4.");
        return Task.FromResult(r);
    }

    private static string? SafeRead(string path) { try { return File.ReadAllText(path); } catch { return null; } }
}
