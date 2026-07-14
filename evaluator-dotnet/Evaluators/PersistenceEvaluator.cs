using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 5 — Persistence &amp; Database (🟠 proxy).
/// Roslyn detects migrations vs EnsureCreated, FK/relationships, indexes and AsNoTracking. The schema
/// itself is exercised for real by the live contract oracle (cat. 1) against the Postgres the submission
/// booted, so a migration that doesn't apply or a broken mapping surfaces as a failed contract check
/// (a 500 instead of a 201), not as a static opinion.</summary>
public sealed class PersistenceEvaluator : CategoryEvaluatorBase
{
    public override int Number => 5;
    public override string Name => "Persistence & Database";
    public override double Weight => 0.13;
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
        r.Metrics.Add(Bool("read-perf", f.Invokes("AsNoTracking", "AsNoTrackingWithIdentityResolution"), "AsNoTracking on reads (efficiency proxy)", weight: 0.5));

        // No `concurrency` (rowversion) metric — and the task no longer asks for one. The API surface is
        // read + create only: there is no UPDATE anywhere in scope, so an optimistic-concurrency token
        // guards against a write conflict that CANNOT HAPPEN. Demanding it was the rubric contradicting
        // its own YAGNI rule — rewarding a pattern with no variation point to justify it.

        r.Notes.Add("PROXY: schema shape (migrations, FKs, indexes) is scored automatically from Roslyn; the schema is then exercised end to end by the live contract oracle.");
        return Task.FromResult(r);
    }
}
