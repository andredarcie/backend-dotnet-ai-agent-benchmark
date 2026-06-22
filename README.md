# Backend .NET — AI Agent Benchmark

**This project is an AI-coding benchmark that gives several models the same prompt to build a .NET 10 Credit Card REST API (Controllers + EF Core + PostgreSQL + Kafka, all in Docker), then automatically scores each submission — by running it in Docker and analyzing its source — on required stack, the Controller → Use Case → Repository architecture, build & boot, functional CRUD/validation/status-codes, Kafka event publishing (verified by consuming the topic), real Postgres persistence, stress/load, and engineering best practices.**

Every model gets **the same prompt** ([`PROMPT.md`](./PROMPT.md)) and must deliver a Credit
Card REST API (.NET 10 + EF Core + PostgreSQL + Kafka, all in Docker). An **automated evaluator**
([`evaluator/`](./evaluator)) then:

1. **Analyzes the source** (static requirements: 2 controllers, 2 entities, ORM, Postgres, Kafka, docker-compose; plus the Controller → Use Case → Repository layering).
2. **Starts the submission's `docker-compose`**.
3. **Hits the API** with functional tests (CRUD, relationship, validation, status codes).
4. **Consumes the Kafka topic** to verify transactions are published as events.
5. **Stresses** the API with concurrent load (RPS, error rate, p95 latency).
6. Produces a **scored report** per category plus a leaderboard comparing the models.

## 🏆 Leaderboard

Final ranking of evaluated submissions. Regenerate any time with `npm run eval -- --leaderboard`
(reads `evaluator/results/*.json`); full source in [`evaluator/results/leaderboard.md`](./evaluator/results/leaderboard.md).

| # | Submission | Total | Static · 30 | Arch · 10 | Boot · 15 | Functional · 25 | Kafka · 20 | Stress · 10 | Quality · 20 | strict-db |
|--:|------------|------:|:-----------:|:---------:|:---------:|:---------------:|:----------:|:-----------:|:------------:|:--------:|
| 1 | `claude-opus-4-8-xhigh` | **124.7 / 130** (95.9%) | 30 | 10 | 15 | 25 | 20 | 10 | **14.7** | ✅ |
| 2 | `gpt-5-5-xhigh` | **124 / 130** (95.4%) | 30 | 10 | 15 | 25 | 20 | 10 | 14 | ✅ |
| 3 | `gemini-3-5-flash` | **112 / 130** (86.2%) | 30 | 10 | 15 | 25 | 20 | 10 | 2 | ✅ |
| 4 | `claude-haiku-4-5` | **104 / 130** (80%) | **25** | 10 | 15 | 25 | **15** | 10 | 4 | ✅ |

> **Safe margin of error: ±5 points (~4%).** Treat submissions within ~5 points of each other as a
> statistical tie. About **120 of the 130 points reproduce exactly run-to-run** (Static, Architecture,
> Functional, Kafka and Quality come from Roslyn + file/HTTP analysis); only the **stress band (~10 pts)**
> is environment-sensitive, and best-of-N already absorbs that noise. With this margin,
> `claude-opus-4-8-xhigh` (124.7) and `gpt-5-5-xhigh` (124) are **effectively tied at #1** — the point
> estimate puts Opus a hair ahead — while `gemini-3-5-flash` (112) and `claude-haiku-4-5` (104) are each
> clearly separated.

All four meet the contract (boot, CRUD, Kafka publish, real Postgres persistence — all verified).
The ranking is decided by the stricter checks added on top:

- **`claude-opus-4-8-xhigh`** and **`gpt-5-5-xhigh`** are a near-perfect tie — both .NET 10, KRaft,
  recent Kafka, durable producer + healthcheck, `CancellationToken`, response DTOs, `Result` errors.
  They trade two checks: **GPT** doesn't hardcode `container_name` (+2), but **Opus** wraps its Kafka
  publish so a broker hiccup doesn't 500 the request (**resilient publish, +3**) — Opus nets ahead by
  **0.7** (GPT's slightly smaller codebase makes up part of the LOC gap). Both still lose `migrations`
  and non-root `USER`.
- **`gemini-3-5-flash`** has a durable Kafka producer (`Acks.All`) and healthcheck, but loses Quality
  (returns entities instead of DTOs, hardcoded `container_name`, Zookeeper, no `CancellationToken`,
  and **rethrows on publish failure**).
- **`claude-haiku-4-5`** is on **.NET 8, not .NET 10** (−5 in Static, a contract violation), and its
  compose has **no Kafka healthcheck** and a **non-durable producer** (−5 in Kafka).

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

Prerequisites: **Docker** (with `docker compose` v2) and **Node.js 20+**.

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

Reports (JSON + Markdown) are written to `evaluator/results/`, and the consolidated
leaderboard to `evaluator/results/leaderboard.md`.

## Typical workflow

1. Hand [`PROMPT.md`](./PROMPT.md) to each model.
2. Save each model's output into `submissions/<model-name>/` (it must have a `docker-compose.yml` at the folder root).
3. Run `npm run eval` (all submissions) or `npm run eval -- <name>` (a single one).
4. Compare the reports under `evaluator/results/`, then refresh the table above with
   `npm run eval -- --leaderboard`.

See [`REQUIREMENTS.md`](./REQUIREMENTS.md) for the full scoring rubric.
