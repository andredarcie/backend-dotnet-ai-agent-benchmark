# Backend .NET — AI Agent Benchmark

One prompt, many models, one automated score. Every model gets the same [`PROMPT.md`](./PROMPT.md) and
must build a **.NET 10 Credit Card REST API** (Controllers + EF Core + PostgreSQL + Kafka, all in
Docker). Each run is generated in **two passes** — a build pass, then a **self-review / validate / patch**
pass ([`PROMPT-REVIEW.md`](./PROMPT-REVIEW.md), the "second chance" every model gets). The
**[.NET evaluator](./evaluator-dotnet)** then walks the **8 scored categories** of
[`EVALUATION-CRITERIA.md`](./EVALUATION-CRITERIA.md) (Roslyn AST + a live contract oracle in deep mode)
and produces a **weighted 0–5 score**.

> **The benchmark grades engineering, not ceremony.** It used to have 13 categories — but three of them
> (Documentation, Portability, Performance) were **6% of the score combined**, arithmetically incapable of
> separating two submissions, while the same signals were counted twice and three times over (health in
> three places, OpenAPI in two, a test project's existence in two). The rubric now measures each thing
> **once, where it is strongest**: 8 weighted categories, 3 reported as *informational*. And it cuts both
> ways — the model is now **penalized for gold-plating** (`no-gold-plating`): shipping a `PUT`, an outbox,
> OpenTelemetry or API versioning that the brief explicitly ruled out is a defect, not ambition.

> 🌐 **Interactive site (PT/EN):** the criteria (with a **per-metric technical breakdown**), the two-pass
> flow, scoring, methodology, per-run **provenance**, and a live leaderboard — explained with diagrams in
> [`docs/`](./docs), published via GitHub Pages at
> **https://andredarcie.github.io/backend-dotnet-ai-agent-benchmark/**.

## 🏆 Leaderboard

> The leaderboard was **reset** for the new rubric: every earlier run was graded under the old
> 13-category rubric and a tool set that no longer exists, so no published score was reproducible by the
> code in this repo. Ranked by **per-model median** of **deep** runs; models with fewer than 5 runs are
> **provisional**.

| # | Model | Score (median /5) | Effort | Time | Cost | Build | Boot |
|--:|-------|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | `sonnet-5` ⚠ | **5.00 / 5** | xhigh | 46m | — | ✅ | ✅ |

⚠ **Provisional (n=1), and a perfect score deserves scepticism, not applause.** The run boots, the live
oracle passes all 17 contract checks, a real event lands on the Kafka topic keyed by id, its own 72 tests
pass, and it covers **72% of the code it wrote** — on a **single pass**, with no self-review. But grading
it also exposed **four bugs in the evaluator**, all of which had punished it for doing the *right* thing:
coverage counted EF migrations and source-generated code the task itself mandates (72% read as 32%);
`application-layer` missed the idiomatic project-per-layer layout; `validation` recognised only
DataAnnotations/FluentValidation, not explicit guard clauses; `graceful-shutdown` knew `BackgroundService`
but not `IHostedService`. All four are fixed and pinned by regression tests. **A rubric that a
first-graded single-pass run tops at 5.00 is not yet discriminating at the top** — the honest reading is
that the ceiling needs raising, not that the problem is solved.

Scores are produced **100% by the `evaluator-dotnet` tool** — no human, no LLM. A run is graded exactly
as the model produced it (**including its own second-pass review**); a build/boot blocker is not patched
by us but **capped by the executability gate**: source doesn't compile **≤ 0.5**, no runnable system
(no `docker-compose.yml`) **≤ 1.0**, compiles but never boots healthy **≤ 1.5**. The cap is a pure
function of how far the submission got, so the ranking is fully reproducible.

### What is scored

| # | Category | Weight | | # | Informational (reported, **not scored**) |
|--:|----------|:------:|-|--:|------------------------------------------|
| 1 | Functional Correctness & Tests 🟡 | **20%** | | 9 | Observability |
| 4 | REST API Design 🟡 | **14%** | | 10 | Portability & Deploy |
| 7 | Security (PCI) 🟠 | **14%** | | 11 | Documentation |
| 5 | Persistence & Database 🟠 | **13%** | | | |
| 6 | Messaging (Kafka) 🟢 | **13%** | | | |
| 2 | Architecture & Design 🟠 | **12%** | | | |
| 3 | Code Quality 🟢 | **10%** | | | |
| 8 | Resilience & Error Handling 🟢 | **4%** | | | |

The heaviest signal by far is the **live contract oracle** in category 1: the evaluator drives the real
API against the real Postgres and Kafka and asserts the documented contract. It is the one thing a model
**cannot write in its own favour** — which is exactly why the submission's own test suite sits *beside*
it, at low weight, instead of standing alone as a category.

Measurement badges (**all 100% automated**, the colour only marks how *directly* a category is measured):
🟢 deterministic · 🟡 oracle · 🟠 proxy. **Per-category scores, the per-metric analysis and the cap
reason** are in each run's report under `evaluator-dotnet/results/`.

## Generating a run — `model-runner`

[`model-runner`](./model-runner) is a cross-platform .NET console app: you name the model, it does the
rest — both passes, in the same folder, plus the provenance sidecar.

```powershell
dotnet run --project model-runner -- haiku-4-5            # next run, 2-pass, writes the meta.json
dotnet run --project model-runner -- gpt-5-5 --effort xhigh
dotnet run --project model-runner -- gemini --no-review   # single pass
dotnet run --project model-runner -- --list              # the model matrix
```

- **Pass 1 — build:** feeds `PROMPT.md`; the model writes the whole project into `submissions/<model>/<run>/`.
- **Pass 2 — review:** feeds `PROMPT.md` + `PROMPT-REVIEW.md`; the model critically reviews its own work
  against the brief, **verifies it however it judges best** (read, build, test, run it…), and applies a
  final patch. The prompt is deliberately open-ended — it tests whether the model knows to validate its
  own work, without leaking *how*.
- **Provenance:** writes `submissions/<model>/<run>.meta.json` (harness, effort, duration, passes,
  tokens, cost) automatically; the site renders it, and captures token/cost from `claude`'s JSON output.
- **Auth:** for `claude`, run with `ANTHROPIC_API_KEY` **unset** so it uses your claude.ai login (a key
  without credit otherwise fails the run). See [`submissions/README.md`](./submissions/README.md).

## Evaluating a run

Needs **Docker** (with `docker compose` v2) and the **.NET 10 SDK**.

```powershell
# light — static analysis only (Roslyn AST + package/file detection), no Docker
dotnet run --project evaluator-dotnet -- haiku-4-5/run1

# deep — boots the submission (app + Postgres + Kafka), drives the live API contract oracle,
#        runs dotnet test/coverage + the bundled tools (SAST/DAST/lint), then writes the report
cd evaluator-dotnet/harness          # set TARGET=<model>/<run> in .env, then:
docker compose up --build --abort-on-container-exit

# rebuild the per-model ranking from existing deep reports
dotnet run --project evaluator-dotnet -- --leaderboard

# refresh the site data after grading (docs/data/*)
./docs/generate-data.ps1
```

Reports land in `evaluator-dotnet/results/<target>.dotnet.{md,json}`.

## More

- 📝 **[PROMPT.md](./PROMPT.md)** — the exact build prompt handed to every model (pass 1).
- 🔁 **[PROMPT-REVIEW.md](./PROMPT-REVIEW.md)** — the second-pass review/validate/patch prompt.
- 📋 **[EVALUATION-CRITERIA.md](./EVALUATION-CRITERIA.md)** — the 13-category rubric (0–5 scale, weights,
  per-category automated method + tools, and how the weighted score is computed).
- 🤖 **[model-runner](./model-runner)** — the cross-platform run generator (2-pass + provenance).
- 🔬 **[evaluator-dotnet/README.md](./evaluator-dotnet/README.md)** — evaluator internals: what each
  category measures in light vs deep mode.
- 🐳 **[evaluator-dotnet/harness/README.md](./evaluator-dotnet/harness/README.md)** — the single-command
  deep harness (boots the stack and grades it against the live system).
- 🗂️ **[submissions/README.md](./submissions/README.md)** — the submission layout and the provenance
  (`<run>.meta.json`) schema.

## Layout

```
.
├── PROMPT.md               # pass-1 build prompt handed to every model
├── PROMPT-REVIEW.md        # pass-2 review/validate/patch prompt (the "second chance")
├── EVALUATION-CRITERIA.md  # the 13-category rubric (0–5, weighted)
├── model-runner/           # cross-platform run generator (2-pass) + provenance
├── submissions/            # <model>/<run> — one folder per run
│   └── <model>/<run>.meta.json   # authored provenance (harness, effort, time, cost)
├── evaluator-dotnet/       # the .NET 10 evaluator (Roslyn AST + live oracle)
│   ├── Evaluators/         # one evaluator per category
│   ├── harness/            # docker-compose deep harness (boots the stack, grades it live)
│   └── results/            # generated reports (<target>.dotnet.md/.json)
└── docs/                   # the interactive site (leaderboard, criteria, provenance)
```
