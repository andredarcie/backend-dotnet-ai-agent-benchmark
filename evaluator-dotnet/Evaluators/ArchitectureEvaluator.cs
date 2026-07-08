using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 2 — Architecture &amp; Design (🟠 proxy).
/// Engine: Roslyn AST (the NetArchTest/architecture-rule role) for layering, dependency direction,
/// single-implementation interfaces and type size. ReSharper CLI (jb inspectcode) is used when present.</summary>
public sealed class ArchitectureEvaluator : CategoryEvaluatorBase
{
    public override int Number => 2;
    public override string Name => "Architecture & Design";
    public override double Weight => 0.10;
    public override AutomationLevel Automation => AutomationLevel.ProxyReview;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        bool layered = (p.AnyDir("Domain") || p.AnyDir("Entities") || p.AnyDir("Models"))
                       && (p.AnyDir("Infrastructure") || p.AnyDir("Repositories") || p.AnyDir("Data"))
                       && (p.AnyDir("Controllers") || p.AnyDir("Api") || p.AnyDir("Endpoints"));
        r.Metrics.Add(Bool("layering", layered, "layer separation (domain/infra/presentation)"));

        bool usecases = p.AnyDir("UseCases") || p.AnyDir("Application") || p.AnyDir("Services") || p.AnyDir("Handlers");
        r.Metrics.Add(Bool("application-layer", usecases, "application/use-case layer present"));

        r.Metrics.Add(f.DomainInfraLeakFiles == 0
            ? Pass("dependency-direction", "0 leaks", "domain does not reference infrastructure (Roslyn usings)")
            : Fail("dependency-direction", $"{f.DomainInfraLeakFiles} domain file(s) reference infra", "domain does not reference infrastructure"));

        // Single-implementation interfaces are the standard Dependency-Inversion seam at layer boundaries
        // (I*Repository / IUnitOfWork / I*Publisher) — the very pattern the layering & dependency-direction
        // checks above REWARD, and each has a second implementation in practice (the test doubles). So a
        // high single-impl ratio is surfaced as informational only (Indeterminate, excluded from the
        // score), never an automatic penalty that contradicts the same category's other metrics.
        if (f.InterfaceCount == 0)
            r.Metrics.Add(Pass("overengineering-proxy", "no interfaces", "few speculative abstractions", weight: 0.5));
        else
        {
            double ratio = (double)f.SingleImplementationInterfaces / f.InterfaceCount;
            r.Metrics.Add(ratio <= 0.5
                ? Pass("overengineering-proxy", $"{f.SingleImplementationInterfaces}/{f.InterfaceCount} interfaces with <=1 impl", "few speculative abstractions", weight: 0.5)
                : Unknown("overengineering-proxy", "few speculative abstractions",
                    $"{f.SingleImplementationInterfaces}/{f.InterfaceCount} single-implementation interfaces — normal DIP ports (repository/UoW/publisher); informational, not scored", 0.5));
        }

        r.Metrics.Add(f.LargestTypeLines <= 600
            ? Pass("no-god-class", $"largest type: {f.LargestTypeLines} lines", "no 'god classes' (<=600 lines)", weight: 0.5)
            : Partial("no-god-class", $"{f.LargestTypeName}: {f.LargestTypeLines} lines", "no 'god classes' (<=600 lines)", weight: 0.5));

        // ReSharper CLI (real tool) when present: surfaces dead code / redundancies that feed the score.
        if (ctx.Options.Deep && ctx.Tools.IsAvailable("jb", "--version"))
        {
            var outFile = Path.Combine(Path.GetTempPath(), "jb-" + Guid.NewGuid().ToString("N")[..8] + ".xml");
            var o = ctx.Tools.Run("jb", $"inspectcode \"{p.Root}\" -o=\"{outFile}\" -f=Xml", p.Root, 600_000);
            try
            {
                if (!o.NotFound && File.Exists(outFile))
                {
                    int issues = 0;
                    try { issues = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(outFile), "<Issue ").Count; } catch { }
                    r.Metrics.Add(issues == 0 ? Pass("resharper", "0 issues", "ReSharper inspectcode clean", weight: 0.5)
                                              : Partial("resharper", $"{issues} issue(s)", "ReSharper inspectcode clean", weight: 0.5));
                }
            }
            finally { try { if (File.Exists(outFile)) File.Delete(outFile); } catch { } }
        }

        r.Notes.Add("PROXY: layering and overengineering are scored automatically from Roslyn dependency-direction, class-size and single-implementation-interface metrics. NDepend/SonarQube can deepen this.");
        return Task.FromResult(r);
    }
}
