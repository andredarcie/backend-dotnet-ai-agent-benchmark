# Backend Evaluator (.NET 10)

A .NET 10 console app that evaluates a backend project (Web API + messaging + database) by walking **the 13 categories** defined in [`../EVALUATION-CRITERIA.md`](../EVALUATION-CRITERIA.md). For each category it runs the corresponding **local** checks/tools, computes a **0–5 score**, applies the weights, and emits the **weighted final score** plus a report (console, Markdown and JSON).

> Self-contained: its only NuGet dependency is **Roslyn** (`Microsoft.CodeAnalysis.CSharp`, for AST analysis) — once restored, the evaluator builds and runs offline. The third-party CLI tools (Spectral, Semgrep, Trivy, sqlfluff, hadolint, k6, …) are **optional** and used only in `--deep` mode; when absent, the category is marked as not measured instead of crashing.

## Usage

```pwsh
# light mode (static analysis + detection; fast, no Docker)
dotnet run --project evaluator-dotnet -- claude-haiku-4-5/run1

# deep mode (also runs the heavy/dynamic tools)
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
| What it does | static analysis of the source + package/file detection | everything in light **plus** runs tools |
| Needs Docker? | no | yes, for dynamic checks |
| Needs network? | no | yes (CVE feeds, `nuget restore`) |
| Extra checks | — | `dotnet test`, coverage (Coverlet), `dotnet format`, SCA (`dotnet list --vulnerable`), Spectral, sqlfluff, hadolint |

## Measurement method (mirror the badges in the .md)

Every category is scored **100% by machine** — no human is ever in the loop. The badge only marks how
*directly* a category is measured:

- 🟢 **deterministic** — scored from static analysis; the same source always produces the same score.
- 🟡 **oracle** — scored each run against a one-time oracle/threshold (correctness suite, expected status codes, SLO).
- 🟠 **proxy** — scored from an objective proxy metric (coupling, rule-violation counts, presence checks). Less direct than a deterministic count, but still fully automated.

## Category → what is measured (summary)

| # | Category | Auto | Main signals (light) | Extra (deep) |
|---|----------|------|----------------------|--------------|
| 1 | Functional Suitability / Correctness | 🟡 | test project, black-box tests, mutation config | **live contract oracle** (drives the flow: 201/`Location`/id, FK/amount/merchant→400, 404s, 204s), `dotnet test` (pass rate), Schemathesis |
| 2 | Architecture & Design | 🟠 | layering, dependency direction, single-impl interfaces, god class | — |
| 3 | Code Quality | 🟢 | empty catch, TODO/FIXME, analyzers/.editorconfig | `dotnet format` |
| 4 | REST API Design | 🟡 | HTTP verbs, status codes, ProblemDetails, OpenAPI, versioning, DTOs | **live**: `Location` header, `application/problem+json`, camelCase, pagination; Spectral lint |
| 5 | Persistence & Database | 🟠 | migrations vs EnsureCreated, FK, indexes, concurrency, AsNoTracking | sqlfluff |
| 6 | Messaging | 🟢 | client, durable producer, idempotent consumer, Outbox, DLQ, offset | **live**: real event observed on the `transactions` topic, keyed by id (harness `kafka-check`) |
| 7 | Security | 🟠 | hardcoded secrets, **PAN (Luhn)**, CVV/track/PIN, authz, validation, rate limit, TLS | `dotnet list --vulnerable`, gitleaks/trivy/semgrep |
| 8 | Resilience & Error Handling | 🟢 | Polly, health checks, global handler, graceful shutdown, timeouts | (Toxiproxy) |
| 9 | Tests | 🟢 | framework, pyramid, Coverlet, Stryker | real coverage |
| 10 | Observability | 🟢 | OpenTelemetry, structured logs, /metrics, correlation, health | — |
| 11 | Performance & Scalability | 🟡 | async I/O, sync-over-async, statelessness, pagination | (k6) |
| 12 | Portability / Deploy | 🟢 | Dockerfile, compose, env config, pinning, CI, non-root | hadolint |
| 13 | Documentation | 🟠 | README + sections, OpenAPI, doc comments | (markdownlint/lychee) |

## How the score is computed

1. Each metric gets **Pass (1.0) / Partial (0.5) / Fail (0.0)**; metrics that could not be measured become **Indeterminate** and are **excluded** (they do not penalize).
2. Category score = weighted mean of the measured metrics × 5 (0–5 scale).
3. The **final score** = mean of the category scores weighted by the `.md` weights, **renormalized** over the categories that produced a score (the report shows the **coverage**).

## Relationship to the existing `../evaluator` (TypeScript)

The `evaluator/` (TS) scores against a **benchmark-requirements** model (7 categories, boots via Docker and tests the live API). This project is **complementary**: it applies the **general quality rubric** of the 13 categories from `EVALUATION-CRITERIA.md`, with strong static analysis in light mode and local tools in deep mode. The two can coexist.

## Project layout

```
evaluator-dotnet/
  Evaluator.csproj          # net10.0, console, no external deps
  Program.cs                # top-level entry: calls Cli.Runner
  Cli/
    Runner.cs               # CLI parsing + orchestration + weighted aggregation
  Core/                     # one type per file
    AutomationLevel.cs, AutomationLevelExtensions.cs, MetricStatus.cs,
    MetricResult.cs, CategoryResult.cs, EvaluationReport.cs,
    ToolOutcome.cs, ToolRunner.cs, ProjectInspector.cs,
    EvaluatorOptions.cs, EvaluationContext.cs,
    ICategoryEvaluator.cs, CategoryEvaluatorBase.cs
  Evaluators/               # one evaluator per file + registry + heuristics
    EvaluatorRegistry.cs, Heuristics.cs,
    FunctionalCorrectnessEvaluator.cs ... DocumentationEvaluator.cs
  Reporting/
    ConsoleReporter.cs, JsonReporter.cs, MarkdownReporter.cs
  results/                  # generated reports
```
