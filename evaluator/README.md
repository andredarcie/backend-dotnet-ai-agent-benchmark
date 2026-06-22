# Evaluator

Automated grader for the .NET Credit Card API benchmark. For each submission it:

1. **Static analysis** (no Docker) — requirements + the Controller → Use Case → Repository layering.
2. **Build & boot** — `docker compose up --build` and wait for the API.
3. **Functional tests** — CRUD, relationship, validation, status codes (per [`../PROMPT.md`](../PROMPT.md)).
4. **Kafka** — consume `localhost:29092` / topic `transactions` and verify a created transaction is published.
5. **Stress** — concurrent load; error rate, throughput, p95.

Scores follow [`../REQUIREMENTS.md`](../REQUIREMENTS.md) (100 points total).

## Install

```bash
npm install      # Node.js 20+
```

## Usage

```bash
npm run eval                 # evaluate every folder in ../submissions
npm run eval -- reference    # evaluate a single submission
npm run eval -- a b c        # evaluate several

# flags
npm run eval -- reference --static-only   # only static + architecture (no Docker)
npm run eval -- reference --no-stress     # skip the load test
npm run eval -- reference --no-kafka      # skip Kafka checks
npm run eval -- reference --keep-up       # leave containers running after the run
npm run eval -- reference --strict-db     # also verify Postgres was actually used (see below)
npm run eval -- reference --retries=2     # retry boot on transient failures (default: 1)
npm run eval -- --leaderboard             # (re)build results/leaderboard.md from saved reports (no Docker)
```

Run the evaluator's own unit tests with `npm test`.

### Static analysis engine (Roslyn)

C# structure checks (categories 1 & 2) use **Roslyn** — real C# syntax trees — so they correctly
understand primary constructors, attributes, comments and partial classes. The first run builds
the analyzer in `analyzer/` (needs the .NET SDK). If the SDK is missing, it transparently falls
back to regex heuristics. Each report row is tagged `[roslyn]` or `[regex]`.

### Robustness

Before each boot the runner removes leftover `bench-*` containers still holding ports 8080/29092
(orphans from a crashed run — it never touches your other containers) and retries boot on transient
failures (`--retries=N`, default 1). The **stress** phase is best-of-N too: if it misses the
thresholds it re-runs (no rebuild) and keeps the best attempt, absorbing transient host-load noise.

### `--strict-db` (runtime Postgres integrity)

Opt-in. After boot, the evaluator finds the submission's Postgres container, reads its
credentials, and runs `psql` to confirm the schema was actually persisted to Postgres
(≥ 2 base tables in the `public` schema). This catches a submission that references the
Npgsql package but secretly runs on an in-memory provider.

It is a **separate VERIFIED/FAILED verdict** shown in the report and console — it does **not**
change the 0–100 score, so runs stay comparable whether or not the flag is used. A `FAILED`
verdict is a strong signal to disqualify a submission manually.

Reports land in `results/`:
- `results/<name>.json` — full machine-readable report
- `results/<name>.md` — human-readable breakdown
- `results/leaderboard.md` — ranked comparison (when > 1 submission)

## Configuration (env vars)

| Var | Default | Meaning |
|-----|---------|---------|
| `BENCH_BASE_URL` | `http://localhost:8080` | API base URL |
| `BENCH_KAFKA` | `localhost:29092` | Kafka bootstrap (host side) |
| `BENCH_KAFKA_TOPIC` | `transactions` | Topic to consume |
| `BENCH_BOOT_MS` | `180000` | How long to wait for the API to become healthy |
| `BENCH_STRESS_CONCURRENCY` | `50` | Concurrent stress workers |
| `BENCH_STRESS_MS` | `15000` | Stress duration |
| `BENCH_STRESS_MAX_ERR` | `0.01` | Max error rate to pass (1%) |
| `BENCH_STRESS_MIN_RPS` | `50` | Min sustained throughput to pass |
| `BENCH_STRESS_MAX_P95` | `1000` | Max p95 latency (ms) to pass |
| `BENCH_UP_MS` | `360000` | `docker compose up` timeout (build included) |

## How it works

- `src/checks/static.ts` + `architecture.ts` — regex analysis over the submission's source files.
- `src/docker.ts` — wraps `docker compose` (isolated project name per submission, `down -v` between runs).
- `src/checks/functional.ts` — scripted HTTP assertions; the functional weight is split evenly across them.
- `src/checks/kafka.ts` — a `kafkajs` consumer verifies the published event.
- `src/checks/stress.ts` — a self-contained concurrent load generator.
- `src/report.ts` — scoring, Markdown/JSON output, leaderboard.

Submissions are evaluated **one at a time**, so they can reuse the same host ports.
