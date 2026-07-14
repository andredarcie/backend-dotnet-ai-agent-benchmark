using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 2 — Architecture &amp; Design (🟠 proxy).
/// Engine: Roslyn AST (the NetArchTest/architecture-rule role) for layering, dependency direction,
/// type size and unrequested machinery.</summary>
public sealed class ArchitectureEvaluator : CategoryEvaluatorBase
{
    public override int Number => 2;
    public override string Name => "Architecture & Design";
    public override double Weight => 0.12;
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

        // YAGNI, made enforceable. The old `overengineering-proxy` counted single-implementation
        // interfaces — but those are the standard Dependency-Inversion seam (I*Repository / I*Publisher)
        // that this very category REWARDS, so it could never fail without contradicting itself, and in
        // practice it was a free pass. It is replaced by the one overengineering signal that is both
        // objective and unambiguous: machinery the brief EXPLICITLY ruled out. Building it is not
        // ambition, it is not following the spec — and the spec says overengineering is a defect.
        var goldPlating = new List<string>();
        if (f.UsesAttribute("HttpPut", "HttpPatch", "HttpDelete") || f.Invokes("MapPut", "MapPatch", "MapDelete"))
            goldPlating.Add("PUT/PATCH/DELETE endpoints (surface is read+create only)");
        if (f.HasOutboxType) goldPlating.Add("transactional outbox (out of scope)");
        if (f.IdentifierEquals("IConsumer", "ConsumeResult") || f.Invokes("Consume", "Subscribe"))
            goldPlating.Add("a Kafka consumer (producer-only is in scope)");
        if (p.HasPackage("OpenTelemetry") || f.UsesNamespace("OpenTelemetry"))
            goldPlating.Add("the OpenTelemetry SDK (explicitly not required)");
        if (f.UsesNamespace("Asp.Versioning") || f.IdentifierEquals("ApiVersion"))
            goldPlating.Add("API versioning (not asked for)");
        if (p.HasPackage("Testcontainers")) goldPlating.Add("Testcontainers (forbidden)");

        r.Metrics.Add(Grade("no-gold-plating",
            goldPlating.Count == 0 ? 1 : goldPlating.Count <= 2 ? 0.5 : 0,
            goldPlating.Count == 0 ? "none" : string.Join("; ", goldPlating),
            "no machinery the brief ruled out (YAGNI)"));

        r.Metrics.Add(f.LargestTypeLines <= 600
            ? Pass("no-god-class", $"largest type: {f.LargestTypeLines} lines", "no 'god classes' (<=600 lines)", weight: 0.5)
            : Partial("no-god-class", $"{f.LargestTypeName}: {f.LargestTypeLines} lines", "no 'god classes' (<=600 lines)", weight: 0.5));

        r.Notes.Add("PROXY: layering, dependency direction, class size and unrequested machinery are all scored automatically from the Roslyn AST.");
        return Task.FromResult(r);
    }
}
