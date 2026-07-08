using System.Text.Json;
using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 7 — Security (🟠 proxy).
/// Tools: gitleaks (secrets), dotnet list --vulnerable / Trivy (SCA), Semgrep (SAST). Built-in PCI
/// checks: PAN (Luhn) over string literals + appsettings, and forbidden CVV/track/PIN fields.</summary>
public sealed class SecurityEvaluator : CategoryEvaluatorBase
{
    public override int Number => 7;
    public override string Name => "Security";
    public override double Weight => 0.12;
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

        // Auth is OPTIONAL and out of scope for this task (no user/ownership model) — so it is reported
        // for visibility but carries zero weight: its absence must not penalize the Security score.
        bool authz = f.UsesAttribute("Authorize") || f.Invokes("AddAuthentication", "AddAuthorization", "RequireAuthorization");
        r.Metrics.Add(authz
            ? Pass("authz", "present", "authentication/authorization (optional — not scored)", "no user/ownership model is in scope", weight: 0)
            : Unknown("authz", "authentication/authorization (optional — not scored)", "out of scope — absence is not a finding", weight: 0));

        r.Metrics.Add(Bool("validation", p.HasPackage("FluentValidation") || f.UsesAttribute("Required", "Range", "StringLength") || f.IdentifierEquals("ModelState"),
            "input validation", weight: 0.5));
        r.Metrics.Add(Bool("rate-limit", f.Invokes("AddRateLimiter", "RequireRateLimiting"), "rate limiting (OWASP API #4)", weight: 0.5));

        // TLS posture for production. The task forbids forcing an HTTPS redirect on the container's HTTP
        // port, so HSTS (prod-guarded) is the right signal; a bare redirect is only a partial credit.
        bool hsts = f.Invokes("UseHsts");
        bool httpsRedirect = f.Invokes("UseHttpsRedirection");
        r.Metrics.Add(hsts
            ? Pass("tls", "HSTS configured", "TLS/HSTS configured for production", weight: 0.5)
            : httpsRedirect
                ? Partial("tls", "HTTPS redirect only", "TLS/HSTS configured for production", "prefer HSTS; don't force redirect on the HTTP port", weight: 0.5)
                : Fail("tls", "none", "TLS/HSTS configured for production", weight: 0.5));

        // Real tool: secret scan (fast, runs in any mode).
        RunTool(ctx, r, "gitleaks", $"detect --source \"{p.Root}\" --no-git --no-banner", "secrets", "no hardcoded secrets (gitleaks)",
            o => o.ExitCode == 0 ? Pass("secrets", "0 leaks", "no hardcoded secrets (gitleaks)")
                                 : Fail("secrets", "leaks found", "no hardcoded secrets (gitleaks)", "review gitleaks findings"));

        // Real tools: SAST / SCA (heavier / need network) -> deep.
        if (ctx.Options.Deep)
        {
            RunTool(ctx, r, "dotnet", "list package --vulnerable --include-transitive", "sca", "0 High/Critical dependencies", o =>
            {
                bool vuln = o.Combined.Contains("has the following vulnerable", StringComparison.OrdinalIgnoreCase)
                            || System.Text.RegularExpressions.Regex.IsMatch(o.Combined, @">\s*(High|Critical)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return vuln ? Fail("sca", "vulnerable packages", "0 High/Critical dependencies")
                            : Pass("sca", "none reported", "0 High/Critical dependencies");
            });
            // Second SCA engine (Trivy): scans the whole tree, not just NuGet, and catches OS/transitive CVEs.
            RunTool(ctx, r, "trivy",
                $"fs --quiet --no-progress --scanners vuln --severity HIGH,CRITICAL --exit-code 1 \"{p.Root}\"",
                "sca-trivy", "0 High/Critical vulnerabilities (Trivy)",
                o => o.Combined.Contains("FATAL", StringComparison.OrdinalIgnoreCase)
                        ? Unknown("sca-trivy", "0 High/Critical vulnerabilities (Trivy)", "Trivy could not run (vuln DB/network?)", 0.5)
                   : o.ExitCode == 0
                        ? Pass("sca-trivy", "none High/Critical", "0 High/Critical vulnerabilities (Trivy)", weight: 0.5)
                        : Fail("sca-trivy", "High/Critical found", "0 High/Critical vulnerabilities (Trivy)", "review Trivy SCA findings", 0.5),
                weight: 0.5, timeoutMs: 300_000);
            // Scope SAST to the APPLICATION: this benchmark's Security criterion is the API's posture (PAN,
            // secrets, injection, validation), per PROMPT.md — not CI supply-chain hygiene. Excluding
            // .github/ keeps semgrep from scoring "pin your GitHub Actions to a SHA", a real but tangential
            // nit the task never asks for.
            RunTool(ctx, r, "semgrep", $"--error --quiet --config auto --exclude .github \"{p.Root}\"", "sast", "no SAST findings (Semgrep)",
                o => o.ExitCode == 0 ? Pass("sast", "clean", "no SAST findings (Semgrep)")
                                     : Partial("sast", "findings", "no SAST findings (Semgrep)"), timeoutMs: 300_000);
        }
        else r.Notes.Add("Run with --deep for SCA (`dotnet list --vulnerable` / Trivy) and SAST (Semgrep). DAST (OWASP ZAP) needs the app running.");

        r.Notes.Add("PROXY: scored automatically from SAST/DAST tool output and the live BOLA oracle scenario (user A vs resource of B) in --deep.");
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
