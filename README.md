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
| 1 | `sonnet-5` | 1 | **4.84 / 5** | ✅ | ✅ |
| 2 | `fable-5` | 1 | **4.80 / 5** | ✅ | ✅ |
| 3 | `gpt-5-5` | 1 | **4.66 / 5** | ✅ | ✅ |
| 4 | `opus-4-8` | 1 | **1.50 / 5** | ✅ | ❌ |
| 5 | `gemini` | 1 | **1.00 / 5** | ✅ | ❌ |
| 6 | `haiku-4-5` | 1 | **0.50 / 5** | ❌ | ❌ |

Scores are produced **100% by the `evaluator-dotnet` tool** — no human, no LLM. A submission is graded
exactly as the model produced it; a build/boot blocker is not patched but capped by the executability
gate (no compile ≤ 0.5, no runnable system ≤ 1.0, never boots ≤ 1.5). That is why `opus-4-8` lands at
1.50 (its Dockerfile `COPY`s `CreditCardApi.sln` while the project ships `.slnx`, so the image never
builds), `gemini` at 1.00 (ships no docker-compose) and `haiku-4-5` at 0.50 (source does not compile).

Measurement badges (**all 100% automated**, the colour only marks how *directly* a category is measured):
🟢 deterministic · 🟡 oracle · 🟠 proxy. **Per-category scores, per-metric analysis and the cap reason**
are in each run's report under
[`evaluator-dotnet/results/`](./evaluator-dotnet/results/) (e.g. the top run,
[`sonnet-5_run1.dotnet.md`](./evaluator-dotnet/results/sonnet-5_run1.dotnet.md)).

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
