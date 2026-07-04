# Backend .NET — AI Agent Benchmark

One prompt, many models, one automated score. Every model gets the same [`PROMPT.md`](./PROMPT.md) and
must build a **.NET 10 Credit Card REST API** (Controllers + EF Core + PostgreSQL + Kafka, all in
Docker). The **[.NET evaluator](./evaluator-dotnet)** walks the **13 categories** of
[`EVALUATION-CRITERIA.md`](./EVALUATION-CRITERIA.md) (Roslyn AST + a live contract oracle in deep mode)
and produces a **weighted 0–5 score**.

> 🌐 **Interactive site (PT/EN):** the criteria, scoring, methodology and a live leaderboard, explained
> with diagrams — in [`docs/`](./docs), publishable via GitHub Pages at
> **https://andredarcie.github.io/backend-dotnet-ai-agent-benchmark/**.

## 🏆 Leaderboard

> Weighted **0–5** across the 13 categories, ranked by **per-model median** of **deep** runs. One model,
> one run so far → **provisional**. _Light/static-only runs aren't comparable._

| # | Model | Runs | Score (median /5) | Build | Boot |
|--:|-------|:---:|:---:|:---:|:---:|
| 1 | `opus-4-8` | 1 | **4.40 / 5** | ✅ | ✅ |

**Per-category — `opus-4-8/run1` (deep):**

| # | Category | Auto | /5 | # | Category | Auto | /5 |
|--:|----------|:--:|:--:|--:|----------|:--:|:--:|
| 1 | Functional / Correctness | 🟡 | 5.0 | 8 | Resilience & Errors | 🟢 | 5.0 |
| 2 | Architecture & Design | 🟠 | 5.0 | 9 | Tests | 🟢 | 5.0 |
| 3 | Code Quality | 🟢 | 3.0 | 10 | Observability | 🟢 | 5.0 |
| 4 | REST API Design | 🟡 | 4.7 | 11 | Performance | 🟡 | 5.0 |
| 5 | Persistence & Database | 🟠 | 5.0 | 12 | Portability & Deploy | 🟢 | 5.0 |
| 6 | Messaging | 🟢 | 5.0 | 13 | Documentation | 🟠 | 4.2 |
| 7 | Security | 🟠 | 5.0 | | | | |

Badges: 🟢 full-auto · 🟡 semi (oracle) · 🟠 proxy + review.
**Per-metric analysis, caps and penalties** are in each run's report →
[`evaluator-dotnet/results/opus-4-8_run1.dotnet.md`](./evaluator-dotnet/results/opus-4-8_run1.dotnet.md).

## Run it

Needs **Docker** (with `docker compose` v2) and the **.NET 10 SDK**.

```powershell
# light — static analysis only (Roslyn AST + package/file detection), no Docker
dotnet run --project evaluator-dotnet -- opus-4-8/run1

# deep — boots the submission (app + Postgres + Kafka), drives the live API contract oracle,
#        runs dotnet test/coverage + the bundled tools (SAST/DAST/lint), then writes the report
cd evaluator-dotnet/harness          # set TARGET=<model>/<run> in .env, then:
docker compose up --build --exit-code-from evaluator

# rebuild the per-model ranking from existing deep reports
dotnet run --project evaluator-dotnet -- --leaderboard
```

Reports land in `evaluator-dotnet/results/<target>.dotnet.{md,json}` (light) or
`evaluator-dotnet/harness/results/` (deep harness).

## More

- 📝 **[PROMPT.md](./PROMPT.md)** — the exact prompt handed to every model.
- 📋 **[EVALUATION-CRITERIA.md](./EVALUATION-CRITERIA.md)** — the 13-category rubric (0–5 scale, weights,
  per-category automated method + tools, and how the weighted score is computed).
- 🔬 **[evaluator-dotnet/README.md](./evaluator-dotnet/README.md)** — evaluator internals: what each
  category measures in light vs deep mode.
- 🐳 **[evaluator-dotnet/harness/README.md](./evaluator-dotnet/harness/README.md)** — the single-command
  deep harness (boots the stack + ZAP + all bundled tools against the live system).

## Layout

```
.
├── PROMPT.md               # the single prompt handed to every model
├── EVALUATION-CRITERIA.md  # the 13-category rubric (0–5, weighted)
├── submissions/            # <model>/<run> — one folder per run
└── evaluator-dotnet/       # the .NET 10 evaluator (Roslyn AST + live oracle)
    ├── Evaluators/         # one evaluator per category
    ├── harness/            # docker-compose deep harness (boots stack + tools)
    └── results/            # generated reports (<target>.dotnet.md/.json)
```
