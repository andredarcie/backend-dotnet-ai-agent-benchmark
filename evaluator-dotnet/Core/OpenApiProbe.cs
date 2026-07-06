using System.Text.Json;

namespace BackendEvaluator.Core;

/// <summary>
/// Locates and inspects the OpenAPI document a live API serves. Probes the well-known paths of both the
/// modern ASP.NET Core native OpenAPI (<c>/openapi/v1.json</c>) and Swashbuckle (<c>/swagger/v1/swagger.json</c>),
/// honoring an explicit <c>BENCH_OPENAPI_PATH</c> first. Beyond finding the URL it counts the declared paths
/// and operations, so callers can tell a real, populated contract apart from one that is served but EMPTY
/// (e.g. <c>AddOpenApi()</c> not discovering the controllers — the doc is 200 but has <c>"paths": {}</c>).
/// </summary>
public static class OpenApiProbe
{
    private static readonly string[] Candidates =
        { "/openapi/v1.json", "/swagger/v1/swagger.json", "/openapi/v1", "/openapi.json", "/swagger.json" };

    public sealed record Doc(string Url, int Paths, int Operations);

    /// <summary>Returns the served OpenAPI doc (URL + path/operation counts), or null if none is served.</summary>
    public static Doc? Discover(string baseUrl)
    {
        var b = baseUrl.TrimEnd('/');
        var candidates = new List<string>();
        var env = Environment.GetEnvironmentVariable("BENCH_OPENAPI_PATH");
        if (!string.IsNullOrWhiteSpace(env)) candidates.Add(env);
        candidates.AddRange(Candidates);

        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var url = b + (path.StartsWith('/') ? path : "/" + path);
            var resp = HttpProbe.Send("GET", url);
            if (!resp.Reached || resp.Status != 200 || string.IsNullOrWhiteSpace(resp.Body)) continue;
            if (TryCount(resp.Body, out int paths, out int ops)) return new Doc(url, paths, ops);
            // 200 but not a parseable OpenAPI document — keep probing the remaining candidates.
        }
        return null;
    }

    private static bool TryCount(string body, out int paths, out int operations)
    {
        paths = 0; operations = 0;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (!root.TryGetProperty("openapi", out _) && !root.TryGetProperty("swagger", out _))
                return false; // not an OpenAPI/Swagger document
            if (root.TryGetProperty("paths", out var p) && p.ValueKind == JsonValueKind.Object)
                foreach (var path in p.EnumerateObject())
                {
                    paths++;
                    if (path.Value.ValueKind == JsonValueKind.Object)
                        foreach (var method in path.Value.EnumerateObject())
                            if (IsHttpMethod(method.Name)) operations++;
                }
            return true;
        }
        catch { return false; }
    }

    private static bool IsHttpMethod(string s) => s.ToLowerInvariant() is
        "get" or "post" or "put" or "patch" or "delete" or "head" or "options" or "trace";
}
