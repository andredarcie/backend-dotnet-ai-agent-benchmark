using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 4 — REST API Design (🟡 semi: status codes need an oracle).
/// Tools: Spectral (OpenAPI lint), swagger-cli (validate), oasdiff (breaking changes). Roslyn detects
/// verbs, status codes, ProblemDetails and DTOs.</summary>
public sealed class ApiDesignEvaluator : CategoryEvaluatorBase
{
    public override int Number => 4;
    public override string Name => "REST API Design";
    public override double Weight => 0.11;
    public override AutomationLevel Automation => AutomationLevel.SemiOracle;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        bool verbs = f.UsesAttribute("HttpGet", "HttpPost", "HttpPut", "HttpPatch", "HttpDelete")
                     || f.Invokes("MapGet", "MapPost", "MapPut", "MapPatch", "MapDelete");
        r.Metrics.Add(Bool("http-verbs", verbs, "correct HTTP verbs (Richardson L2)"));

        bool statusCodes = f.IdentifierEquals("StatusCodes") || f.Invokes("Created", "CreatedAtAction", "Ok", "NoContent", "BadRequest", "NotFound", "Conflict", "UnprocessableEntity");
        r.Metrics.Add(Bool("status-codes", statusCodes, "explicit, coherent status codes"));

        bool problem = f.ObjectCreationTypes.Any(t => t.Contains("ProblemDetails")) || f.Invokes("AddProblemDetails", "Problem")
                       || f.IdentifierEquals("IExceptionHandler");
        r.Metrics.Add(Bool("problem-details", problem, "standardized errors RFC 9457 (ProblemDetails)"));

        bool openapi = p.HasPackage("Swashbuckle") || p.HasPackage("NSwag") || p.HasPackage("Microsoft.AspNetCore.OpenApi")
                       || f.Invokes("AddOpenApi", "AddSwaggerGen", "MapOpenApi", "UseSwagger");
        r.Metrics.Add(Bool("openapi", openapi, "OpenAPI/Swagger contract exposed"));

        bool versioning = f.UsesNamespace("Asp.Versioning") || f.IdentifierEquals("ApiVersion");
        r.Metrics.Add(Bool("versioning", versioning, "API versioning", weight: 0.5));

        bool dtos = p.AnyDir("Dtos") || p.AnyDir("DTOs") || f.TypeNameContains("Request", "Response", "Dto");
        r.Metrics.Add(Bool("dtos", dtos, "separates DTOs from domain entities", weight: 0.5));

        // Live contract oracle: the actual response shape (Location header, Problem Details media type,
        // camelCase, pagination) observed against the running system — not inferred from source.
        if (ctx.Contract is { Reachable: true } contract)
            foreach (var check in contract.Checks.Where(c => c.Area == BackendEvaluator.Core.ContractArea.RestDesign))
                r.Metrics.Add(check.ToMetric());

        // The served OpenAPI document must actually DOCUMENT the API. The static `openapi` metric above
        // only proves the middleware is wired; a doc that is served but declares zero operations
        // (e.g. AddOpenApi() failing to discover the controllers -> "paths": {}) is an empty, useless
        // contract — a real API-design defect that presence-detection alone silently passes.
        if (!string.IsNullOrEmpty(ctx.Options.BaseUrl))
        {
            var doc = OpenApiProbe.Discover(ctx.Options.BaseUrl);
            if (doc == null)
                r.Metrics.Add(Unknown("openapi-populated", "served OpenAPI documents its operations", "no OpenAPI document served — not scored"));
            else if (doc.Operations > 0)
                r.Metrics.Add(Pass("openapi-populated", $"{doc.Operations} operation(s) across {doc.Paths} path(s)", "served OpenAPI documents its operations"));
            else
                r.Metrics.Add(Fail("openapi-populated", "0 operations (empty paths)", "served OpenAPI documents its operations",
                    "the OpenAPI document is served but declares no endpoints — an empty contract"));
        }

        // Real tools on a spec file, when one is present.
        var spec = p.FindByNamePattern(@"^(openapi|swagger)\.(json|ya?ml)$").FirstOrDefault();
        if (spec != null)
        {
            RunTool(ctx, r, "spectral", $"lint \"{spec}\" -f json", "spectral", "OpenAPI lint with no errors (Spectral)", o =>
            {
                int errors = System.Text.RegularExpressions.Regex.Matches(o.Stdout, "\"severity\"\\s*:\\s*0").Count;
                return errors == 0 ? Pass("spectral", "0 errors", "OpenAPI lint with no errors (Spectral)")
                                   : Fail("spectral", $"{errors} error(s)", "OpenAPI lint with no errors (Spectral)");
            });
            RunTool(ctx, r, "swagger-cli", $"validate \"{spec}\"", "swagger-validate", "OpenAPI spec is valid (swagger-cli)",
                o => o.Success ? Pass("swagger-validate", "valid", "OpenAPI spec is valid (swagger-cli)")
                               : Fail("swagger-validate", "invalid", "OpenAPI spec is valid (swagger-cli)"), weight: 0.5);
        }
        else r.Notes.Add("No static OpenAPI file found (spec is likely generated at runtime); run --deep to generate and lint it.");

        if (ctx.Contract is not { Reachable: true })
            r.Notes.Add("SEMI: pass --base-url (the harness does) to assert live status codes / Location / Problem Details / pagination.");
        return Task.FromResult(r);
    }
}
