using System.Text.Json;

namespace BackendEvaluator.Core;

/// <summary>Which category a contract check feeds: functional correctness (1), REST design (4) or resilience (8).</summary>
public enum ContractArea { Functional, RestDesign, Resilience }

/// <summary>One assertion made against the live API; maps 1:1 to a category metric.</summary>
public sealed record ContractCheck(
    string Name, MetricStatus Status, string Observed, string Target,
    ContractArea Area, double Weight = 1, string? Note = null)
{
    public MetricResult ToMetric() => new()
    {
        Name = Name, Observed = Observed, Target = Target,
        Status = Status, Weight = Weight, Note = Note,
    };
}

/// <summary>The result of driving the live API end to end.</summary>
public sealed class ContractReport
{
    public bool Reachable { get; set; }
    public string? RoutesBase { get; set; }
    public List<ContractCheck> Checks { get; } = new();
}

/// <summary>
/// Black-box acceptance oracle: drives the documented Credit-Card / Transaction flow against the
/// running API and asserts the real request→response contract (status codes, <c>Location</c>, the
/// validation rules, camelCase, Problem Details, pagination). This is the independent reference the
/// static checks and the self-declared OpenAPI cannot provide.
/// </summary>
/// <remarks>
/// This oracle is intentionally opinionated about the documented credit-card / transaction contract.
/// It assumes integer / numeric ids (<see cref="TryGetId"/> returns a <c>long</c> and
/// <see cref="MissingId"/> is the numeric constant <c>999_000_111</c>) and the fixed request schema
/// <c>brand:"VISA"</c> with field names <c>cardholderName</c>, <c>cardNumber</c>, <c>creditLimit</c>,
/// <c>creditCardId</c>, <c>amount</c> and <c>merchant</c>. An API keyed by GUID or using a different
/// shape will silently fail these checks — that is by design, not a bug.
/// </remarks>
public static class ContractOracle
{
    // An id large enough that it should never exist on a freshly-migrated database.
    private const long MissingId = 999_000_111;

    public static ContractReport Run(string baseUrl, Action<string>? log = null)
    {
        var report = new ContractReport();
        baseUrl = baseUrl.TrimEnd('/');

        var (cards, txs, query) = DiscoverRoutes(baseUrl);
        if (cards is null || txs is null)
        {
            report.Reachable = false;
            return report;
        }
        report.Reachable = true;
        report.RoutesBase = cards;
        log?.Invoke($"    contract oracle: driving {cards} and {txs}");

        string Q(string url) => query is null ? url : url + (url.Contains('?') ? "&" : "?") + query;
        var c = report.Checks;

        // ---------- Credit card: create + validation + camelCase + Location ----------
        const string cardJson = """{"cardholderName":"Ada Lovelace","cardNumber":"4111111111111111","brand":"VISA","creditLimit":5000.00}""";
        var createCard = HttpProbe.Send("POST", Q(cards), cardJson);
        c.Add(Status("create-card-201", createCard, 201, "POST credit-card -> 201 Created", ContractArea.Functional));
        c.Add(Bool("create-card-location", createCard.Reached && !string.IsNullOrWhiteSpace(createCard.Location),
            createCard.Location ?? "(none)", "201 carries a Location header", ContractArea.RestDesign, 0.5));
        long cardId = TryGetId(createCard.Body);
        c.Add(Bool("create-card-id", cardId > 0, cardId > 0 ? $"id={cardId}" : "(no id in body)",
            "create response returns the new id", ContractArea.Functional));
        c.Add(CamelCase("json-camelcase", createCard.Body));

        var emptyCard = HttpProbe.Send("POST", Q(cards), """{"cardholderName":"","cardNumber":"","creditLimit":100}""");
        c.Add(Status("card-required-400", emptyCard, 400, "empty cardholderName/cardNumber -> 400", ContractArea.Functional));
        c.Add(ProblemJson("problem-details-live", emptyCard));

        // Resilience: a malformed request must be handled (4xx) without leaking a stack trace / internals.
        var malformed = HttpProbe.Send("POST", Q(cards), "{ \"cardholderName\": \"x\", ");
        c.Add(NoLeak("no-stacktrace-leak", malformed));

        // A second card so pagination has more than one row to slice.
        HttpProbe.Send("POST", Q(cards), cardJson);
        c.Add(Status("list-cards-200", HttpProbe.Send("GET", Q(cards)), 200, "GET credit-cards collection -> 200", ContractArea.Functional, 0.5));
        c.Add(Pagination(cards, Q));

        // ---------- Credit card: read by id / 404 ----------
        if (cardId > 0)
            c.Add(Status("get-card-200", HttpProbe.Send("GET", Q($"{cards}/{cardId}")), 200, "GET existing card -> 200", ContractArea.Functional));
        c.Add(Status("get-card-404", HttpProbe.Send("GET", Q($"{cards}/{MissingId}")), 404, "GET missing card -> 404", ContractArea.Functional));

        // ---------- Transaction: create + echo + Location ----------
        long txId = 0;
        if (cardId > 0)
        {
            string txJson = $$"""{"creditCardId":{{cardId}},"amount":199.90,"merchant":"Amazon","category":"shopping"}""";
            var createTx = HttpProbe.Send("POST", Q(txs), txJson);
            c.Add(Status("create-tx-201", createTx, 201, "POST transaction -> 201 Created", ContractArea.Functional));
            c.Add(Bool("create-tx-location", createTx.Reached && !string.IsNullOrWhiteSpace(createTx.Location),
                createTx.Location ?? "(none)", "201 carries a Location header", ContractArea.RestDesign, 0.5));
            txId = TryGetId(createTx.Body);
            c.Add(Bool("create-tx-id", txId > 0, txId > 0 ? $"id={txId}" : "(no id in body)",
                "create response returns the new id", ContractArea.Functional));
            c.Add(EchoTx("create-tx-echo", createTx.Body));
        }

        // ---------- Transaction: the business rules (FK, amount, merchant) ----------
        if (cardId > 0)
        {
            c.Add(Status("tx-amount-positive-400", HttpProbe.Send("POST", Q(txs),
                $$"""{"creditCardId":{{cardId}},"amount":-5,"merchant":"Acme"}"""), 400,
                "amount <= 0 -> 400", ContractArea.Functional, 1.5));
            c.Add(Status("tx-merchant-required-400", HttpProbe.Send("POST", Q(txs),
                $$"""{"creditCardId":{{cardId}},"amount":10,"merchant":""}"""), 400,
                "empty merchant -> 400", ContractArea.Functional, 1.5));
        }
        c.Add(Status("tx-fk-exists-400", HttpProbe.Send("POST", Q(txs),
            $$"""{"creditCardId":{{MissingId}},"amount":10,"merchant":"Acme"}"""), 400,
            "non-existent creditCardId -> 400", ContractArea.Functional, 1.5));

        // ---------- Transaction: read collection / by id / 404, and the card's transactions ----------
        c.Add(Status("list-tx-200", HttpProbe.Send("GET", Q(txs)), 200, "GET transactions collection -> 200", ContractArea.Functional, 0.5));
        if (txId > 0)
            c.Add(Status("get-tx-200", HttpProbe.Send("GET", Q($"{txs}/{txId}")), 200, "GET existing transaction -> 200", ContractArea.Functional));
        c.Add(Status("get-tx-404", HttpProbe.Send("GET", Q($"{txs}/{MissingId}")), 404, "GET missing transaction -> 404", ContractArea.Functional));
        if (cardId > 0)
            c.Add(Status("card-transactions-200", HttpProbe.Send("GET", Q($"{cards}/{cardId}/transactions")), 200,
                "GET card's transactions -> 200", ContractArea.Functional));
        c.Add(Status("card-transactions-404", HttpProbe.Send("GET", Q($"{cards}/{MissingId}/transactions")), 404,
            "transactions of a missing card -> 404", ContractArea.Functional, 0.5));

        // No PUT/DELETE section: the task's API surface is read + create only, so asserting update/delete
        // would penalize a submission for not building what it was never asked to build.
        return report;
    }

    /// <summary>Finds the route prefix the API actually serves (handles /api, versioning, etc.).</summary>
    private static (string? cards, string? txs, string? query) DiscoverRoutes(string baseUrl)
    {
        string[] prefixes = { "/api/v1", "/api", "/v1", "/api/v1.0", "" };
        string?[] queries = { null, "api-version=1.0", "api-version=1" };
        foreach (var q in queries)
            foreach (var p in prefixes)
            {
                string url = baseUrl + p + "/credit-cards";
                var probe = HttpProbe.Send("GET", q is null ? url : url + "?" + q);
                if (probe.Reached && probe.Status == 200)
                    return (baseUrl + p + "/credit-cards", baseUrl + p + "/transactions", q);
            }
        return (null, null, null);
    }

    // ---- check builders ----

    private static ContractCheck Status(string name, HttpProbe.Response r, int expected, string target, ContractArea area, double weight = 1)
        => new(name, r.Reached && r.Status == expected ? MetricStatus.Pass : MetricStatus.Fail,
               r.Reached ? $"HTTP {r.Status}" : "unreachable", target, area, weight);

    private static ContractCheck Bool(string name, bool ok, string observed, string target, ContractArea area, double weight = 1)
        => new(name, ok ? MetricStatus.Pass : MetricStatus.Fail, observed, target, area, weight);

    private static ContractCheck ProblemJson(string name, HttpProbe.Response r)
        => new(name, r.ContentType?.Contains("problem+json", StringComparison.OrdinalIgnoreCase) == true ? MetricStatus.Pass : MetricStatus.Fail,
               r.ContentType ?? "(none)", "errors use application/problem+json (RFC 9457)", ContractArea.RestDesign, 0.5);

    // Markers that strongly indicate a leaked .NET exception / stack trace in an error body.
    private static readonly string[] LeakMarkers =
        { "StackTrace", "   at ", ".cs:line", "EntityFrameworkCore", "Npgsql.", "InnerException", "DbUpdateException" };

    private static ContractCheck NoLeak(string name, HttpProbe.Response r)
    {
        if (!r.Reached) return new(name, MetricStatus.Indeterminate, "unreachable", "errors don't leak stack traces / internals", ContractArea.Resilience);
        bool serverError = r.Status >= 500;
        bool leak = !string.IsNullOrEmpty(r.Body) && LeakMarkers.Any(m => r.Body.Contains(m, StringComparison.OrdinalIgnoreCase));
        var status = serverError || leak ? MetricStatus.Fail : MetricStatus.Pass;
        string observed = serverError ? $"HTTP {r.Status} (unhandled)" : leak ? "stack trace in body" : $"HTTP {r.Status}, clean body";
        return new(name, status, observed, "errors don't leak stack traces / internals", ContractArea.Resilience);
    }

    private static ContractCheck CamelCase(string name, string body)
    {
        var (ok, sample) = AllKeysCamelCase(body);
        return new(name, ok ? MetricStatus.Pass : MetricStatus.Fail,
                   ok ? "camelCase" : $"non-camelCase: {sample}", "JSON properties are camelCase", ContractArea.RestDesign, 0.5);
    }

    private static ContractCheck EchoTx(string name, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = Unwrap(doc.RootElement);
            bool amountOk = TryGetProp(root, "amount", out var amt) && amt.ValueKind == JsonValueKind.Number && amt.GetDecimal() == 199.90m;
            bool merchOk = TryGetProp(root, "merchant", out var m) && m.ValueKind == JsonValueKind.String && m.GetString() == "Amazon";
            return new(name, amountOk && merchOk ? MetricStatus.Pass : MetricStatus.Fail,
                       amountOk && merchOk ? "amount+merchant echoed" : "mismatch", "create echoes the persisted fields", ContractArea.Functional, 0.5);
        }
        catch
        {
            return new(name, MetricStatus.Fail, "unparseable body", "create echoes the persisted fields", ContractArea.Functional, 0.5);
        }
    }

    private static ContractCheck Pagination(string cardsUrl, Func<string, string> Q)
    {
        const string target = "collection honors a page size";
        string[] qs = { "pageSize=1", "pageSize=1&pageNumber=1", "pageSize=1&page=1", "limit=1", "perPage=1", "size=1&page=0" };
        foreach (var qp in qs)
        {
            string baseQ = Q(cardsUrl);
            var r = HttpProbe.Send("GET", baseQ + (baseQ.Contains('?') ? "&" : "?") + qp);
            if (!r.Reached || r.Status != 200) continue;
            var (count, hasMeta) = ExtractCollection(r.Body);
            // An empty page (count == 0) is NOT evidence of pagination — the collection was seeded with 2 cards,
            // so a working page size of 1 must return exactly 1 row. count == 0 falls through to the next style.
            if (count is 1) return new("pagination", MetricStatus.Pass, $"{qp} -> {count} item(s)", target, ContractArea.RestDesign, 0.5);
            if (hasMeta) return new("pagination", MetricStatus.Pass, "paging metadata present", target, ContractArea.RestDesign, 0.5);
            if (count is > 1) return new("pagination", MetricStatus.Fail, $"{qp} -> {count} items (page size ignored)", target, ContractArea.RestDesign, 0.5);
        }
        return new("pagination", MetricStatus.Partial, "could not confirm", target, ContractArea.RestDesign, 0.5, "verify pagination manually");
    }

    // ---- JSON helpers (also unit-tested) ----

    /// <summary>True when every property name of the JSON object is camelCase. Returns a failing sample otherwise.</summary>
    internal static (bool ok, string sample) AllKeysCamelCase(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = Unwrap(doc.RootElement);
            if (root.ValueKind != JsonValueKind.Object) return (true, "");
            foreach (var prop in root.EnumerateObject())
            {
                string k = prop.Name;
                if (k.Length == 0) continue;
                if (char.IsUpper(k[0]) || k.Contains('_')) return (false, k);
            }
            return (true, "");
        }
        catch
        {
            return (true, ""); // not JSON / not an object — don't penalize here
        }
    }

    /// <summary>Pulls an integer <c>id</c> out of a create response (handles a wrapping envelope).</summary>
    internal static long TryGetId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = Unwrap(doc.RootElement);
            if (TryGetProp(root, "id", out var id))
            {
                if (id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var n)) return n;
                if (id.ValueKind == JsonValueKind.String && long.TryParse(id.GetString(), out var s)) return s;
            }
            return -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>Returns (item count, whether paging metadata is present) for an array or an envelope.</summary>
    internal static (int? count, bool hasMeta) ExtractCollection(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array) return (root.GetArrayLength(), false);
            if (root.ValueKind == JsonValueKind.Object)
            {
                bool hasMeta = false;
                foreach (var key in new[] { "page", "pageNumber", "pageSize", "total", "totalCount", "totalPages", "hasNext", "count" })
                    if (TryGetProp(root, key, out _)) { hasMeta = true; break; }
                foreach (var arrayKey in new[] { "items", "data", "results", "content", "value", "records" })
                    if (TryGetProp(root, arrayKey, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        return (arr.GetArrayLength(), hasMeta);
                return (null, hasMeta);
            }
            return (null, false);
        }
        catch
        {
            return (null, false);
        }
    }

    /// <summary>Unwraps a single-property {"data": {...}} / {"value": {...}} envelope around the entity.</summary>
    private static JsonElement Unwrap(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
            foreach (var key in new[] { "data", "value", "result", "item" })
                if (TryGetProp(root, key, out var inner) && inner.ValueKind == JsonValueKind.Object)
                    return inner;
        return root;
    }

    /// <summary>Case-insensitive property lookup (APIs may answer in camelCase or PascalCase).</summary>
    private static bool TryGetProp(JsonElement obj, string name, out JsonElement value)
    {
        if (obj.ValueKind == JsonValueKind.Object)
            foreach (var prop in obj.EnumerateObject())
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
        value = default;
        return false;
    }
}
