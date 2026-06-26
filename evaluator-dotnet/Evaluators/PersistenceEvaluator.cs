using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 5 — Persistence &amp; Database (🟠 proxy + review).
/// Tools: sqlfluff (SQL lint). Roslyn detects migrations vs EnsureCreated, FK/relationships, indexes,
/// concurrency tokens and AsNoTracking. EXPLAIN/SchemaCrawler need a live DB (--deep).</summary>
public sealed class PersistenceEvaluator : CategoryEvaluatorBase
{
    public override int Number => 5;
    public override string Name => "Persistence & Database";
    public override double Weight => 0.10;
    public override AutomationLevel Automation => AutomationLevel.ProxyReview;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        bool migrations = p.AnyDir("Migrations") || f.IdentifierEquals("MigrationBuilder") || p.FindByNamePattern(@"\.sql$").Any();
        bool ensureCreated = f.Invokes("EnsureCreated", "EnsureCreatedAsync");
        r.Metrics.Add(Grade("migrations", migrations && !ensureCreated ? 1 : migrations ? 0.5 : 0,
            ensureCreated ? "uses EnsureCreated" : migrations ? "versioned migrations" : "no migrations",
            "schema evolves via migrations (not EnsureCreated)"));

        r.Metrics.Add(Bool("referential-integrity", f.Relationship, "referential integrity (FK/relationships)"));
        r.Metrics.Add(Bool("indexes", f.Invokes("HasIndex", "CreateIndex"), "indexes defined (incl. FKs/queries)", weight: 0.5));
        r.Metrics.Add(Bool("concurrency", f.UsesAttribute("Timestamp", "ConcurrencyCheck") || f.IdentifierEquals("IsRowVersion", "RowVersion") || f.Invokes("IsConcurrencyToken"),
            "concurrency control (optimistic)", weight: 0.5));
        r.Metrics.Add(Bool("read-perf", f.Invokes("AsNoTracking", "AsNoTrackingWithIdentityResolution"), "AsNoTracking on reads (efficiency proxy)", weight: 0.5));

        // Real tool: lint SQL files when present.
        var sqlFiles = p.FindByNamePattern(@"\.sql$").ToList();
        if (sqlFiles.Count > 0)
            RunTool(ctx, r, "sqlfluff", $"lint --dialect postgres \"{Path.GetDirectoryName(sqlFiles[0])}\"", "sqlfluff", "SQL with no violations (sqlfluff)",
                o => o.Success ? Pass("sqlfluff", "clean", "SQL with no violations (sqlfluff)", weight: 0.5)
                               : Partial("sqlfluff", "violations found", "SQL with no violations (sqlfluff)", weight: 0.5), weight: 0.5);

        if (ctx.Options.Deep && !ctx.Tools.IsAvailable("schemacrawler")) r.MissingTools.Add("schemacrawler");
        r.Notes.Add("PROXY: 3NF / justified denormalization need functional-dependency analysis (human review). N+1, seq scans and EXPLAIN need a live DB (--deep + container, e.g. via pg_stat_statements/HypoPG).");
        return Task.FromResult(r);
    }
}
