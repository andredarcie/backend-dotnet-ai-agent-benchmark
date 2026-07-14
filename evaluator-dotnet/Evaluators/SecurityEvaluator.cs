using System.Text.Json;
using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 7 — Security (🟠 proxy).
/// Tools: gitleaks (secrets), `dotnet list package --vulnerable` (SCA). Built-in PCI checks: PAN (Luhn)
/// over string literals + appsettings, and forbidden CVV/track/PIN fields.</summary>
public sealed class SecurityEvaluator : CategoryEvaluatorBase
{
    public override int Number => 7;
    public override string Name => "Security";
    public override double Weight => 0.14;
    public override AutomationLevel Automation => AutomationLevel.ProxyReview;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        // PCI DSS Req. 3: probable stored PANs across PRODUCTION string literals + appsettings values.
        // Test projects are excluded — standard fake test card numbers are fixtures, not stored PANs.
        var literals = f.ProductionStringLiterals.Concat(AppSettingsValues(p)).ToList();
        int pans = Heuristics.ProbableCardNumbers(literals);
        r.Metrics.Add(pans == 0
            ? Pass("pci-pan", "0", "no PAN (Luhn-valid card) embedded in code/config")
            : Fail("pci-pan", $"{pans} Luhn-valid sequence(s)", "no embedded PAN (PCI DSS Req.3)", "in production source (test fixtures excluded)"));

        bool forbidden = f.IdentifierContains("cvv", "cvc", "cardverification", "track2", "pinblock");
        r.Metrics.Add(forbidden
            ? Fail("pci-sad", "cvv/cvc/track/pin field(s) present", "no sensitive auth data stored (CVV/track/PIN)", "review whether persisted")
            : Pass("pci-sad", "absent", "no sensitive auth data stored (CVV/track/PIN)"));

        // No `authz` metric. Auth is optional and out of scope here (no user/ownership model, so no BOLA
        // to test), and the metric carried weight 0 — it could not move the score under any input. A
        // zero-weight metric is a report line pretending to be a measurement; if it is worth reporting,
        // it is a note.
        bool authz = f.UsesAttribute("Authorize") || f.Invokes("AddAuthentication", "AddAuthorization", "RequireAuthorization");
        if (authz) r.Notes.Add("FYI: authentication/authorization is wired. It is optional and out of scope in this task (no user/ownership model) — neither rewarded nor penalized.");

        // Validation must be detected by its PRESENCE, not by its style. Recognizing only FluentValidation,
        // DataAnnotations and ModelState scored one idiom and failed every other: a submission that
        // validates with explicit guard clauses in the application layer and surfaces the result as a
        // ValidationException / ValidationProblemDetails is validating — arguably more cleanly, since the
        // domain stays free of System.ComponentModel — and the live oracle proves it (the required-field,
        // amount>0 and FK checks all come back 400). Marking that "no input validation" was the evaluator
        // grading a convention it happened to know.
        bool validation = p.HasPackage("FluentValidation")
                          || f.UsesAttribute("Required", "Range", "StringLength")
                          || f.IdentifierEquals("ModelState")
                          || f.IdentifierContains("ValidationException", "ValidationProblem")
                          || f.ObjectCreationTypes.Any(t => t.Contains("ValidationException") || t.Contains("ValidationProblem"));
        r.Metrics.Add(Bool("validation", validation, "input validation", weight: 0.5));
        r.Metrics.Add(Bool("rate-limit", f.Invokes("AddRateLimiter", "RequireRateLimiting"), "rate limiting (OWASP API #4)", weight: 0.5));

        // Real tool: secret scan (fast, runs in any mode).
        RunTool(ctx, r, "gitleaks", $"detect --source \"{p.Root}\" --no-git --no-banner", "secrets", "no hardcoded secrets (gitleaks)",
            o => o.ExitCode == 0 ? Pass("secrets", "0 leaks", "no hardcoded secrets (gitleaks)")
                                 : Fail("secrets", "leaks found", "no hardcoded secrets (gitleaks)", "review gitleaks findings"));

        // Real tool: SCA over the restored NuGet graph -> deep. This is the ONE check that still reaches
        // the network: the vulnerability data comes from the NuGet audit source (nuget.org).
        if (ctx.Options.Deep)
        {
            RunTool(ctx, r, "dotnet", "list package --vulnerable --include-transitive", "sca", "0 High/Critical dependencies", o =>
            {
                // A SILENT FALSE PASS to guard against: with no reachable NuGet source, the command still
                // EXITS 0 and prints "has no vulnerable packages **given the current sources**" — with an
                // empty source list. Read literally that is "we checked nothing", not "nothing is wrong",
                // so it must be Indeterminate (excluded), never a free Pass. Detect it by requiring at
                // least one source line under the "The following sources were used:" banner.
                bool anySource = System.Text.RegularExpressions.Regex.IsMatch(
                    o.Combined, @"sources were used:\s*\r?\n\s+\S", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (!anySource)
                    return Unknown("sca", "0 High/Critical dependencies",
                        "no NuGet source was reachable — vulnerability data unavailable, so nothing was actually checked");

                bool vuln = o.Combined.Contains("has the following vulnerable", StringComparison.OrdinalIgnoreCase)
                            || System.Text.RegularExpressions.Regex.IsMatch(o.Combined, @">\s*(High|Critical)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return vuln ? Fail("sca", "vulnerable packages", "0 High/Critical dependencies")
                            : Pass("sca", "none reported", "0 High/Critical dependencies");
            });
        }
        else r.Notes.Add("Run with --deep for SCA (`dotnet list package --vulnerable`).");

        r.Notes.Add("PROXY: scored automatically from the PCI checks (Luhn PAN / CVV / track / PIN over the Roslyn AST), gitleaks and the NuGet vulnerability graph.");
        return Task.FromResult(r);
    }

    private static IEnumerable<string> AppSettingsValues(ProjectInspector p)
    {
        foreach (var file in p.FindByNamePattern(@"^appsettings.*\.json$"))
        {
            if (p.IsTestFile(file)) continue; // test config is a fixture, not production secrets
            string text;
            try { text = File.ReadAllText(file); } catch { continue; }
            JsonDocument doc;
            try { doc = JsonDocument.Parse(text); } catch { continue; }
            foreach (var v in Walk(doc.RootElement)) yield return v;
        }
    }

    private static IEnumerable<string> Walk(JsonElement e)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.String:
                var s = e.GetString();
                if (s != null) yield return s;
                break;
            case JsonValueKind.Object:
                foreach (var prop in e.EnumerateObject())
                    foreach (var v in Walk(prop.Value)) yield return v;
                break;
            case JsonValueKind.Array:
                foreach (var item in e.EnumerateArray())
                    foreach (var v in Walk(item)) yield return v;
                break;
        }
    }
}
