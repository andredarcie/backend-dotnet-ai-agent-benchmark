# Backend .NET - AI Agent Benchmark

**This project is an AI-coding benchmark that gives several models the same prompt to build a .NET 10 Credit Card REST API, then automatically scores each submission - by running it in Docker and analyzing its source - on required stack, the Controller → Use Case → Repository architecture, build & boot, functional CRUD/validation/status-codes, Kafka event publishing (verified by consuming the topic), real Postgres persistence, stress/load, and engineering best practices.**

Every model gets **the same prompt** ([`PROMPT.md`](./PROMPT.md)) and must deliver a Credit
Card REST API (.NET 10 + EF Core + PostgreSQL + Kafka, all in Docker). An **automated evaluator**
([`evaluator/`](./evaluator)) then:

1. **Analyzes the source** (static requirements: 2 controllers, 2 entities, ORM, Postgres, Kafka, docker-compose; plus the Controller → Use Case → Repository layering).
2. **Starts the submission's `docker-compose`**.
3. **Hits the API** with functional tests (CRUD, relationship, validation, status codes).
4. **Consumes the Kafka topic** to verify transactions are published as events.
5. **Stresses** the API with concurrent load (RPS, error rate, p95 latency).
6. Produces a **scored report** per category plus a leaderboard comparing the models.

> 🧭 **New here?** Follow the [step-by-step tutorial](./TUTORIAL.md): install the tools, grade a bundled sample, then benchmark your own model.

## 🏆 Leaderboard

Regenerate any time with `npm run eval -- --leaderboard`
(reads `evaluator/results/*.json`); full source in [`evaluator/results/leaderboard.md`](./evaluator/results/leaderboard.md).

> ℹ️ **Single-run results under the current 126-point rubric** (Roslyn engine; `--strict-db` verified for
> every submission that booted). These are one run per model - see the methodology note below on why a
> single run should not be read as a definitive ranking, and prefer per-model medians over multiple runs.

| # | Submission | Total | Static · 28 | Arch · 10 | Boot · 15 | Functional · 25 | Kafka · 20 | Stress · 10 | Quality · 18 | strict-db |
|--:|------------|------:|:-----------:|:---------:|:---------:|:---------------:|:----------:|:-----------:|:------------:|:--------:|
| 1 | `claude-opus-4-8-xhigh` | **121 / 126** (96%) | 28 | 10 | 15 | 25 | 20 | 10 | **13** | ✅ |
| 2 | `gpt-5-5-xhigh` | **120 / 126** (95.2%) | 28 | 10 | 15 | 25 | 20 | 10 | 12 | ✅ |
| 3 | `claude-sonnet-4-6-xhigh` | **117 / 126** (92.9%) | **25** | 10 | 15 | 25 | 20 | 10 | 12 | ✅ |
| 4 | `gemini-3-5-flash` | **108 / 126** (85.7%) | 28 | 10 | 15 | 25 | 20 | 10 | 0 | ✅ |
| 5 | `claude-haiku-4-5` | **102 / 126** (81%) | **25** | 10 | 15 | 25 | **15** | 10 | 2 | ✅ |

> ℹ️ `claude-sonnet-4-6-xhigh` needed a **compose patch to boot**: it pinned `bitnami/kafka:3.7`, a tag
> Bitnami removed from Docker Hub (no longer resolves). The Kafka service was swapped to
> `apache/kafka:3.9.0` (env vars translated 1:1 + single-node `__consumer_offsets` replication settings)
> so the project runs - the .NET source was **not** touched. With that, it booted clean and scored full
> Functional/Kafka/Stress. It still targets **.NET 9**, not 10 (−3 in Static), and its only EF
> **migrations** in the field is a quality highlight (`Migrations/` folder, +2 where the others lost it).

### Reading these results (methodology)

The **static** categories - Static, Architecture, Quality, and the static Kafka checks - are
**deterministic given the Roslyn engine**: re-running the analyzer on the same source produces the same
scores. The **runtime** categories - Build/boot, Functional, runtime Kafka, and Stress - depend on
Docker and the host, so they **vary run-to-run**.

Because the models themselves are stochastic, **a single submission per model is a weak sample** and the
table above should not be read as a definitive ranking. For a sound comparison, generate **multiple runs
per model** (folder convention `submissions/<model>__run1/`, `submissions/<model>__run2/`, …) and compare
the **per-model median total**. Treat small gaps - well within the run-to-run spread you actually observe
- as **ties**, rather than ranking by sub-point differences. **Stress is the highest-variance category**
and is scored by the **conservative median across attempts**.

For what it's worth, all four example submissions did meet the core contract (boot, CRUD, Kafka publish,
and real Postgres persistence - all verified). Beyond that baseline, the differences are best understood
through the per-category checks in [`REQUIREMENTS.md`](./REQUIREMENTS.md) rather than a single total.

## Layout

```
.
├── PROMPT.md            # The single prompt handed to every model
├── REQUIREMENTS.md      # Rubric: every requirement and how it is scored
├── submissions/         # One folder per model
│   └── <model-name>/    # Drop each model's generated project here (e.g. gpt-5-5-xhigh/)
└── evaluator/           # Test runner + analyzer (Node.js + TypeScript)
```

## How to run

Prerequisites: **Docker** (with `docker compose` v2), **Node.js 20+**, and the **.NET 10 SDK**
(Roslyn analysis is required by default; pass `--allow-regex-fallback` to skip it).

```powershell
# 1. Install evaluator dependencies (once)
cd evaluator
npm install

# 2. Evaluate ONE submission
npm run eval -- gpt-5-5-xhigh

# 3. Or evaluate ALL submissions in submissions/
npm run eval

# Also verify it really persisted to Postgres (opt-in integrity check):
npm run eval -- gpt-5-5-xhigh --strict-db

# (Re)build the leaderboard from saved reports (no Docker):
npm run eval -- --leaderboard

# Shortcut from the repo root:
./run.ps1 gpt-5-5-xhigh
```

**Roslyn is required by default.** The static analysis uses Roslyn (via the .NET SDK), so the
.NET SDK must be installed; if it is unavailable the evaluation fails fast rather than silently
degrading. Pass `--allow-regex-fallback` to disable that requirement and fall back to regex-based
analysis - each report records which engine was used.

Reports (JSON + Markdown) are written to `evaluator/results/`, and the consolidated
leaderboard to `evaluator/results/leaderboard.md`.

## Typical workflow

1. Hand [`PROMPT.md`](./PROMPT.md) to each model.
2. Save each model's output into `submissions/<model-name>/` (it must have a `docker-compose.yml` at the folder root).
3. Run `npm run eval` (all submissions) or `npm run eval -- <name>` (a single one).
4. Compare the reports under `evaluator/results/`, then refresh the table above with
   `npm run eval -- --leaderboard`.

### Multiple runs per model (recommended)

Models are **stochastic**: the same prompt yields a different project each time. A single submission
per model is a weak sample, and a one- or two-point gap between models is almost certainly noise. For
a defensible comparison, generate **≥ 5 runs per model** and let the leaderboard report the
**median ± standard deviation**.

It's three commands. Re-prompt the model with [`PROMPT.md`](./PROMPT.md) **k times** (fresh each time),
saving each generation to its own folder, then file and score them:

```powershell
cd evaluator

# File each fresh generation as the next run of the model (copies it into submissions/<model>__runN/,
# skipping bin/obj/node_modules). Run this once per generation:
npm run add-run -- gpt-5-5-xhigh ../path/to/generation-1
npm run add-run -- gpt-5-5-xhigh ../path/to/generation-2
# … repeat up to run 5+ (and the same for every other model)

# Score everything and build the aggregated leaderboard:
npm run eval                       # evaluates every submissions/* folder (all runs of all models)
npm run eval -- --leaderboard      # rebuild leaderboard.md (median ± σ per model)
```

The leaderboard groups `submissions/<model>__runN/` folders by model, **ranks by median total**, and
shows **±σ, mean, range and run count**. Any model with fewer than 5 runs is flagged ⚠ and the table
is labelled **provisional**. A bare `submissions/<model>/` folder still works as a one-run sample
(n = 1) and counts as run #1.

> Naming is just the `__` separator: `gpt-5-5-xhigh__run3` → grouped under `gpt-5-5-xhigh`. `add-run`
> picks the next free number for you; you can also create the folders by hand.

## Adding a new model

Want to benchmark a model that isn't here yet? The evaluator auto-discovers any folder under
`submissions/`, so adding a model is just "drop a project in, run the eval". Step by step:

**1. Generate the submission.** Hand the model the **entire** [`PROMPT.md`](./PROMPT.md) verbatim -
nothing else, no hints, no rubric (the rubric lives in [`REQUIREMENTS.md`](./REQUIREMENTS.md) and must
**not** be shown to the model, or you're teaching to the test). Let it produce the whole project.

**2. Create the folder.** Save the model's output into a new directory whose name is the model id in
kebab-case:

```
submissions/<model-name>/          # e.g. submissions/claude-sonnet-4-6/
```

- Use only `[a-z0-9-]` (it becomes the Docker project name `bench-<model-name>`).
- For several runs of the same model (recommended - see above), append `__runN`:
  `submissions/claude-sonnet-4-6__run1/`, `…__run2/`, … They are grouped under `claude-sonnet-4-6`
  in the leaderboard.

**3. Check the folder contract.** The evaluator expects, **at the folder root**:

- `docker-compose.yml` (or `.yaml` / `compose.y(a)ml`) that brings up **API + Postgres + Kafka** with
  one command.
- A `Dockerfile` for the API.
- The API listening on **`8080`** (mapped to host `8080`) and Kafka reachable from the host on
  **`localhost:29092`**.
- The .NET source (project file, `Program.cs`, the 2 controllers, 2 entities, `DbContext`, repository
  + use-case layers, Kafka producer).

> The folder must be **self-contained and boot with a single `docker compose up --build`** - no manual
> setup steps. If your model emitted instructions ("then run migrations…"), the submission fails the
> "no manual step" contract.

**4. Evaluate it.**

```powershell
cd evaluator
npm install                          # first time only
npm run eval -- <model-name>         # e.g. npm run eval -- claude-sonnet-4-6
# from the repo root you can also: ./run.ps1 <model-name>
```

This boots the stack in an isolated Docker project, runs the static + functional + Kafka + stress
checks, then tears it down. Add `--strict-db` to also verify real Postgres persistence.

**5. Read the report & refresh the leaderboard.**

```powershell
npm run eval -- --leaderboard        # rebuild leaderboard.md from saved results (no Docker)
```

Per-model reports land in `evaluator/results/<model-name>.{json,md}`; the combined ranking in
`evaluator/results/leaderboard.md`.

**Prerequisites & gotchas**

- **Docker** (with `docker compose` v2) running, plus the **.NET SDK** (Roslyn analysis is required by
  default - pass `--allow-regex-fallback` to skip it).
- Free host ports **8080** and **29092** - the runner clears stale `bench-*` containers automatically,
  but will warn (and not boot) if a non-benchmark container is holding them.
- If boot fails, categories 4-6 score 0; check the `compose up failed: …` tail in the console and the
  `Notes` section of the report.
- Don't commit `bin/`, `obj/`, or `node_modules/` inside a submission - they're skipped by the analyzer
  anyway, but they bloat the repo.

See [`REQUIREMENTS.md`](./REQUIREMENTS.md) for the full scoring rubric, including the
[Weighting rationale](./REQUIREMENTS.md#weighting-rationale) for the category point split.
