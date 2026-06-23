# Backend .NET - AI Agent Benchmark

One prompt, many models, one automated score. Every model gets the same [`PROMPT.md`](./PROMPT.md) and
must build a **.NET 10 Credit Card REST API** (Controllers + EF Core + PostgreSQL + Kafka, all in
Docker). The [evaluator](./evaluator) boots each submission in Docker, hits every endpoint, consumes the
Kafka topic, stress-tests it, and analyzes the source with Roslyn - producing a **126-point** score
across 7 categories.

## 🏆 Leaderboard

> ℹ️ Current 126-point rubric, ranked by **per-model median total**. Run counts vary
> (`claude-sonnet-4-6-xhigh` and `claude-haiku-4-5` have **3** each, the rest **1**); all are under 5
> runs, so this is a **provisional** ranking. Per-run details and how to read these numbers:
> [METHODOLOGY.md](./METHODOLOGY.md).

| # | Model | Runs | Total (median) | Static · 28 | Arch · 10 | Boot · 15 | Functional · 25 | Kafka · 20 | Stress · 10 | Quality · 18 | strict-db |
|--:|-------|:---:|------:|:-----------:|:---------:|:---------:|:---------------:|:----------:|:-----------:|:------------:|:--------:|
| 1 | `claude-opus-4-8-xhigh` | 1 | **121 / 126** (96%) | 28 | 10 | 15 | 25 | 20 | 10 | **13** | ✅ |
| 2 | `gpt-5-5-xhigh` | 1 | **120 / 126** (95.2%) | 28 | 10 | 15 | 25 | 20 | 10 | 12 | ✅ |
| 3 | `gemini-3-5-flash` | 1 | **108 / 126** (85.7%) | 28 | 10 | 15 | 25 | 20 | 10 | 0 | ✅ |
| 4 | `claude-haiku-4-5` | **3** | **97 / 126** (77%) ±32.7 (43-102) | 28 | 10 | 15 | 25 | 5 | 10 | 4 | ✅ |
| 5 | `claude-sonnet-4-6-xhigh` | **3** | **47 / 126** (37.3%) ±40.4 (47-117) | 25 | 10 | 0 | 0 | 3 | 0 | 9 | run1 ✅ |

Regenerate with `npm run eval -- --leaderboard` (full source:
[`evaluator/results/leaderboard.md`](./evaluator/results/leaderboard.md)). Multi-run rows show the
median; cells show the median/representative run. Why Sonnet and Haiku rank low despite strong single
runs: [METHODOLOGY.md](./METHODOLOGY.md).

## Run it

Needs **Docker** (with `docker compose` v2), **Node.js 20+**, and the **.NET 10 SDK**.

```powershell
cd evaluator && npm install      # once
npm run eval                     # grade every submission in ../submissions
npm run eval -- --leaderboard    # rebuild the ranking
```

## More

- 🧭 **[TUTORIAL.md](./TUTORIAL.md)** - install, grade a sample, benchmark your own model, run k >= 5.
- 📋 **[REQUIREMENTS.md](./REQUIREMENTS.md)** - the full 126-point scoring rubric.
- 📝 **[PROMPT.md](./PROMPT.md)** - the exact prompt handed to every model.
- 🔬 **[METHODOLOGY.md](./METHODOLOGY.md)** - deterministic vs runtime scoring, why multi-run, per-run notes.

## Layout

```
.
├── PROMPT.md         # the single prompt handed to every model
├── REQUIREMENTS.md   # the scoring rubric
├── TUTORIAL.md       # step-by-step usage
├── METHODOLOGY.md    # how to read the results
├── submissions/      # <model>/run1, run2, ... (one folder per run)
└── evaluator/        # test runner + Roslyn analyzer (Node + TypeScript)
```
