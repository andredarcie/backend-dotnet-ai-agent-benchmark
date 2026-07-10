# Backend .NET — AI Agent Benchmark

One prompt, many models, one automated score. Every model gets the same [`PROMPT.md`](./PROMPT.md) and
must build a **.NET 10 Credit Card REST API** (Controllers + EF Core + PostgreSQL + Kafka, all in
Docker). Each run is generated in **two passes** — a build pass, then a **self-review / validate / patch**
pass ([`PROMPT-REVIEW.md`](./PROMPT-REVIEW.md), the "second chance" every model gets). The
**[.NET evaluator](./evaluator-dotnet)** then walks the **13 categories** of
[`EVALUATION-CRITERIA.md`](./EVALUATION-CRITERIA.md) (Roslyn AST + a live contract oracle in deep mode)
and produces a **weighted 0–5 score**.

> 🌐 **Interactive site (PT/EN):** the criteria (with a **per-metric technical breakdown**), the two-pass
> flow, scoring, methodology, per-run **provenance**, and a live leaderboard — explained with diagrams in
> [`docs/`](./docs), published via GitHub Pages at
> **https://andredarcie.github.io/backend-dotnet-ai-agent-benchmark/**.

## 🏆 Leaderboard

> Weighted **0–5** across the 13 categories, ranked by **per-model median** of **deep** runs. The
> leaderboard was **reset** to the new **two-pass** standard — every earlier single-pass run was removed,
> so only `haiku-4-5` is here for now. The site also shows each run's **effort, time and cost**.

| # | Model | Score (median /5) | Effort | Time | Cost | Build | Boot |
|--:|-------|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | `haiku-4-5` | **1.50 / 5** | — | 26m 37s | $1.96 | ✅ | ❌ |

Scores are produced **100% by the `evaluator-dotnet` tool** — no human, no LLM. A run is graded exactly
as the model produced it (**including its own second-pass review**); a build/boot blocker is not patched
by us but **capped by the executability gate**: source doesn't compile **≤ 0.5**, no runnable system
(no `docker-compose.yml`) **≤ 1.0**, compiles but never boots healthy **≤ 1.5**. The cap is a pure
function of how far the submission got, so the ranking is fully reproducible.

`haiku-4-5` compiles locally but hits the **boot-fail cap (1.5)**: its `Dockerfile` `COPY`s the
`.csproj` and runs `dotnet restore` **without** first copying `Directory.Build.props` (which defines the
`TargetFramework`), so the Docker image fails to build (`NETSDK1013`) and the API never boots. The
two-pass review is a **static** pass (no `docker compose up`), so it couldn't catch a Docker-build defect
that a local build doesn't reproduce.

Measurement badges (**all 100% automated**, the colour only marks how *directly* a category is measured):
🟢 deterministic · 🟡 oracle · 🟠 proxy. **Per-category scores, the per-metric analysis and the cap
reason** are in each run's report under [`evaluator-dotnet/results/`](./evaluator-dotnet/results/).

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
- **Pass 2 — review:** feeds `PROMPT.md` + `PROMPT-REVIEW.md`; the model re-reads its code against the
  brief, confirms it compiles, and applies a final patch — a **fast, static review, no Docker**.
- **No Docker needed to generate** (the review pass is static); Docker is only needed later, to *grade*.
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
  deep harness (boots the stack + ZAP + all bundled tools against the live system).
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
│   ├── harness/            # docker-compose deep harness (boots stack + tools + ZAP)
│   └── results/            # generated reports (<target>.dotnet.md/.json)
└── docs/                   # the interactive site (leaderboard, criteria, provenance)
```
