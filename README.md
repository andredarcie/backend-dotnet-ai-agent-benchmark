# Backend .NET - AI Agent Benchmark

One prompt, many models, one automated score. Every model gets the same [`PROMPT.md`](./PROMPT.md) and
must build a **.NET 10 Credit Card REST API** (Controllers + EF Core + PostgreSQL + Kafka, all in
Docker). The [evaluator](./evaluator) boots each submission in Docker, hits every endpoint, consumes the
Kafka topic, stress-tests it, and analyzes the source with Roslyn - producing a **126-point** score
across 7 categories.

## 🏆 Leaderboard

> ℹ️ Current 126-point rubric, ranked by **per-model median total**. Run counts vary
> (`claude-sonnet-4-6-xhigh` and `claude-haiku-4-5` have **3** each, the rest **1**); all are under 5
> runs, so this is a **provisional** ranking. Some Sonnet/Haiku runs were **minimally patched to
> build/boot** (wrong dependency version/name, broken Kafka healthcheck) and carry a **-10 penalty** so
> their real work is still scored. `gemini-3-5-pro` is a heavier patch - it shipped **no
> `docker-compose.yml`/`Dockerfile` at all** (headless single-turn generation), so a standard compose +
> Dockerfile were supplied to boot its code, same **-10 penalty** (and it was not `--strict-db` verified).
> Per-run details and policy: [METHODOLOGY.md](./METHODOLOGY.md).

Category columns are grouped to stay readable: **Static+Arch** (stack · 28 + layering · 10 = 38),
**Runtime** (boot · 15 + functional · 25 + stress · 10 = 50), **Kafka** (20), **Quality** (18).

| # | Model | Runs | Total (median) | Static+Arch · 38 | Runtime · 50 | Kafka · 20 | Quality · 18 |
|--:|-------|:---:|------:|:---------------:|:------------:|:----------:|:------------:|
| 1 | `claude-opus-4-8-xhigh` | 1 | **121 / 126** (96%) | 38 | 50 | 20 | **13** |
| 2 | `gpt-5-5-xhigh` | 1 | **120 / 126** (95.2%) | 38 | 50 | 20 | 12 |
| 3 | `gemini-3-5-flash` | 1 | **108 / 126** (85.7%) | 38 | 50 | 20 | 0 |
| 4 | `gemini-3-5-pro` | 1 | **103 / 126** (81.7%) | 38 | 50 | 16 | 9 |
| 5 | `claude-sonnet-4-6-xhigh` | **3** | **102 / 126** (81%) ±30.6 (58-117) | 35 | 50 | 18 | 9 |
| 6 | `claude-haiku-4-5` | **3** | **97 / 126** (77%) ±14.9 (74-102) | 38 | 50 | 5 | 4 |

Regenerate with `npm run eval -- --leaderboard` (full source:
[`evaluator/results/leaderboard.md`](./evaluator/results/leaderboard.md)). Multi-run rows show the
median (cells = the median/representative run) and the spread. How patched runs are handled and why
the spread is wide: [METHODOLOGY.md](./METHODOLOGY.md).

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
