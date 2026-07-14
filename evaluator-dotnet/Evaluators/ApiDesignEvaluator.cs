using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 4 — REST API Design (🟡 semi: status codes need an oracle).
/// Roslyn detects verbs, status codes, ProblemDetails and DTOs; the live contract oracle asserts the real
/// response shape and OpenApiProbe checks the served document actually declares the operations.</summary>
public sealed class ApiDesignEvaluator : CategoryEvaluatorBase
{
    public override int Number => 4;
    public override string Name => "REST API Design";
    public override double Weight => 0.14;
    public override AutomationLevel Automation => AutomationLevel.SemiOracle;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        bool verbs = f.UsesAttribute("HttpGet", "HttpPost", "HttpPut", "HttpPatch", "HttpDelete")
                     || f.Invokes("MapGet", "MapPost", "MapPut", "MapPatch", "MapDelete");
        r.Metrics.Add(Bool("http-verbs", verbs, "correct HTTP verbs (Richardson L2)"));

        // No static `status-codes` metric: "the source mentions BadRequest somewhere" is a strictly weaker
        // proxy for the real 201/400/404 the live oracle already asserts (category 1). No `versioning`
        // metric either — the task no longer asks for it (see `no-gold-plating` in category 2).
        bool problem = f.ObjectCreationTypes.Any(t => t.Contains("ProblemDetails")) || f.Invokes("AddProblemDetails", "Problem")
                       || f.IdentifierEquals("IExceptionHandler");
        r.Metrics.Add(Bool("problem-details", problem, "standardized errors RFC 9457 (ProblemDetails)"));

        bool dtos = p.AnyDir("Dtos") || p.AnyDir("DTOs") || f.TypeNameContains("Request", "Response", "Dto");
        r.Metrics.Add(Bool("dtos", dtos, "separates DTOs from domain entities", weight: 0.5));

        // Live contract oracle: the actual response shape (Location header, Problem Details media type,
        // camelCase, pagination) observed against the running system — not inferred from source.
        if (ctx.Contract is { Reachable: true } contract)
            foreach (var check in contract.Checks.Where(c => c.Area == BackendEvaluator.Core.ContractArea.RestDesign))
                r.Metrics.Add(check.ToMetric());

        // The OpenAPI contract, asserted on the RUNNING system — this replaces the old static `openapi`
        // presence metric entirely (which only proved the middleware was wired, and which Documentation
        // scored a second time as `api-docs`). Three outcomes, all of them scored:
        //   served + declares operations -> Pass;  served but empty ("paths": {}) -> Fail (a useless
        //   contract that presence-detection silently passed);  NOT SERVED AT ALL -> Fail, not
        //   Indeterminate: the task requires an OpenAPI document, so its absence is a defect, not a
        //   measurement we failed to take.
        if (!string.IsNullOrEmpty(ctx.Options.BaseUrl))
        {
            var doc = OpenApiProbe.Discover(ctx.Options.BaseUrl);
            if (doc == null)
                r.Metrics.Add(Fail("openapi-populated", "no document served", "served OpenAPI documents its operations",
                    "no OpenAPI/Swagger document is served on the running system"));
            else if (doc.Operations > 0)
                r.Metrics.Add(Pass("openapi-populated", $"{doc.Operations} operation(s) across {doc.Paths} path(s)", "served OpenAPI documents its operations"));
            else
                r.Metrics.Add(Fail("openapi-populated", "0 operations (empty paths)", "served OpenAPI documents its operations",
                    "the OpenAPI document is served but declares no endpoints — an empty contract"));
        }

        if (ctx.Contract is not { Reachable: true })
            r.Notes.Add("ORACLE: pass --base-url (the harness does) to assert live status codes / Location / Problem Details / pagination / OpenAPI.");
        return Task.FromResult(r);
    }
}
