# Backend Evaluator (.NET 10)

A .NET 10 console app that evaluates a backend project (Web API + messaging + database) by walking the categories defined in [`../EVALUATION-CRITERIA.md`](../EVALUATION-CRITERIA.md) — **8 that are scored** (their weights sum to 100%) and **3 reported as informational**. For each category it runs the corresponding **local** checks/tools, computes a **0–5 score**, applies the weights, and emits the **weighted final score** plus a report (console, Markdown and JSON).

> Self-contained. Its only NuGet dependency is **Roslyn** (`Microsoft.CodeAnalysis.CSharp`, for AST analysis, run in-process), and the only tools it shells out to are the **.NET SDK** (`build`, `test` + Coverlet, `format`, `list package --vulnerable`), **gitleaks** (secrets) and **hadolint** (Dockerfile lint) — all three bundled in the image. The dynamic signals come from the evaluator's own **live HTTP probes / contract oracle** and, under the harness, the **kcat** sidecar. When a tool is absent the metric is marked *not measured* instead of crashing.
>
> The heavyweights this image used to bundle (OWASP ZAP, Semgrep, Trivy, Schemathesis, Spectral, swagger-cli, sqlfluff, markdownlint, lychee, dotnet-outdated, Stryker.NET, Toxiproxy, k6) are **retired**: they dominated the wall-clock and tied the score to remote rule sets and CVE feeds that drift, so the same source could be graded differently on two different days. What is left is deterministic — same source ⇒ same score — with **one honest exception**: `dotnet list package --vulnerable` reads NuGet's vulnerability data over the network, and that data moves as CVEs are disclosed. It is kept because it costs seconds, and it is reported *Indeterminate* (never a silent Pass) when no NuGet source is reachable.

## Usage

```pwsh
# light mode (static analysis + detection; fast, no Docker)
dotnet run --project evaluator-dotnet -- claude-haiku-4-5/run1

# deep mode (also builds, tests and covers the code)
dotnet run --project evaluator-dotnet -- claude-haiku-4-5/run1 --deep

# arbitrary path target + custom output folder
dotnet run --project evaluator-dotnet -- C:\path\to\project --output C:\tmp\report
```

The target can be a **submission name** (`<model>/<run>`, resolved under `../submissions/`) or **a folder path**. With no argument it lists the available submissions.

Output (in `evaluator-dotnet/results/` by default):
- `<target>.dotnet.md` — human-readable report (summary table + per-category detail)
- `<target>.dotnet.json` — structured report (for leaderboards/automation)

## light vs deep

| | light (default) | deep (`--deep`) |
|---|---|---|
| What it does | static analysis of the source + package/file detection (+ gitleaks) | everything in light **plus** builds/runs the code |
| Needs Docker? | no | only to boot the system under test (the harness does that) |
| Needs network? | no | `nuget restore` of the submission + NuGet's vulnerability data for the SCA check — no remote rule sets, no tool downloads |
| Extra checks | — | `dotnet build` (warnings), `dotnet test` (pass rate), coverage (Coverlet), `dotnet format`, SCA (`dotnet list package --vulnerable`), hadolint |

The **live** checks (contract oracle, `/health` + `/metrics` probes, OpenAPI probe, Kafka ingest) turn on when `--base-url` / `--ingest` are set — which is exactly what the [harness](harness/README.md) does.

## Measurement method (mirror the badges in the .md)

Every category is scored **100% by machine** — no human is ever in the loop. The badge only marks how
*directly* a category is measured:

- 🟢 **deterministic** — scored from static analysis; the same source always produces the same score.
- 🟡 **oracle** — scored each run against a one-time oracle/threshold (correctness suite, expected status codes, SLO).
- 🟠 **proxy** — scored from an objective proxy metric (coupling, rule-violation counts, presence checks). Less direct than a deterministic count, but still fully automated.

## Category → what is measured (summary)

**Scored — the weights sum to 100%:**

| # | Category | W | Auto | Main signals (light) | Extra (deep / live) |
|---|----------|--:|------|----------------------|---------------------|
| 1 | Functional Correctness & **Tests** | 20% | 🟡 | real unit tests (`[Fact]`/`[Theory]`), **unit-only** (Testcontainers → Fail, WebApplicationFactory → Partial) | **live contract oracle** — carries most of the weight (201/`Location`/id, FK/amount/merchant→400, 404s, collections→200); `dotnet test` once → pass rate + **Coverlet** line coverage |
| 2 | Architecture & Design | 12% | 🟠 | layering, dependency direction, god class, **`no-gold-plating`** (PUT/DELETE, outbox, consumer, OTel, versioning, Testcontainers) | — |
| 3 | Code Quality | 10% | 🟢 | empty catch, TODO/FIXME, analyzers/.editorconfig, **async I/O**, **sync-over-async** | `dotnet format`, build warnings |
| 4 | REST API Design | 14% | 🟡 | HTTP verbs, ProblemDetails, DTOs | **live**: `Location`, `application/problem+json`, camelCase, pagination, and `openapi-populated` (no doc served, or an empty one, is a **Fail**) |
| 5 | Persistence & Database | 13% | 🟠 | migrations vs EnsureCreated, FK, indexes, AsNoTracking | — (the schema is exercised end to end by the live oracle) |
| 6 | Messaging (producer-only) | 13% | 🟢 | Kafka client, publish call, durable producer (Acks.All/idempotence) | **live**: real event observed on the `transactions` topic, keyed by id (harness `kafka-check`) |
| 7 | Security | 14% | 🟠 | **PAN (Luhn)**, CVV/track/PIN, validation, rate limit, gitleaks | SCA: `dotnet list package --vulnerable` |
| 8 | Resilience & Error Handling | 4% | 🟢 | Polly, global handler, graceful shutdown, timeouts | **live**: a malformed request leaks no stack trace |

**Informational — measured and reported, but excluded from the score** (at 1–4% they could never separate two submissions, and each duplicated a signal the executability gate or the live oracle already decides):

| # | Category | Auto | Signals |
|---|----------|------|---------|
| 9 | Observability | 🟢 | structured JSON logs, correlation id, **live** `/health` |
| 10 | Portability / Deploy | 🟢 | Dockerfile, compose, env config, pinning, non-root, hadolint |
| 11 | Documentation | 🟠 | README + its sections |

## How the score is computed

1. Each metric gets **Pass (1.0) / Partial (0.5) / Fail (0.0)**; metrics that could not be measured become **Indeterminate** and are **excluded** (they do not penalize).
2. Category score = weighted mean of the measured metrics × 5 (0–5 scale).
3. The **final score** = mean of the category scores weighted by the `.md` weights, **renormalized** over the categories that produced a score (the report shows the **coverage**). **Weight-0 categories are informational**: they are printed, but they are excluded from the weighted mean *and* from the coverage denominator.

## Project layout

```
evaluator-dotnet/
  Evaluator.csproj          # net10.0, console; Roslyn is the only NuGet dependency
  Program.cs                # top-level entry: calls Cli.Runner
  Cli/
    Runner.cs               # CLI parsing + orchestration + weighted aggregation
  Core/                     # one type per file
    AutomationLevel.cs, AutomationLevelExtensions.cs, MetricStatus.cs,
    MetricResult.cs, CategoryResult.cs, EvaluationReport.cs,
    ToolOutcome.cs, ToolRunner.cs, ToolCatalog.cs, ProjectInspector.cs,
    RoslynAnalyzer.cs, CodeFacts.cs, CoberturaCoverage.cs,
    ContractOracle.cs, HttpProbe.cs, OpenApiProbe.cs,   # the live checks
    EvaluatorOptions.cs, EvaluationContext.cs,
    ICategoryEvaluator.cs, CategoryEvaluatorBase.cs
  Evaluators/               # one evaluator per file + registry + heuristics
    EvaluatorRegistry.cs, Heuristics.cs,
    FunctionalCorrectnessEvaluator.cs ... DocumentationEvaluator.cs
  Reporting/
    ConsoleReporter.cs, JsonReporter.cs, MarkdownReporter.cs
  results/                  # generated reports
```
