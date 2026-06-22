# Tutorial

A step-by-step walkthrough: install the tools, grade a bundled sample, then benchmark your own
model. Assumes you're comfortable with a terminal. For the scoring rubric see
[`REQUIREMENTS.md`](./REQUIREMENTS.md); for the project overview see [`README.md`](./README.md).

## 1. Prerequisites

Install these, then reopen your terminal so they're on `PATH`:

| Tool | Why | Get it | Verify |
|------|-----|--------|--------|
| **Docker Desktop** | runs each submission (API + Postgres + Kafka) | <https://www.docker.com/products/docker-desktop/> | `docker --version` |
| **Node.js 20+** | runs the grader | <https://nodejs.org> (LTS) | `node --version` |
| **.NET 10 SDK** | Roslyn source analysis (required by default) | <https://dotnet.microsoft.com/download> | `dotnet --version` |

Start Docker Desktop and wait until it reports **"Engine running"** before grading anything.

## 2. Get the project + one-time setup

```bash
git clone <repo-url>          # or download the ZIP from GitHub and unzip
cd backend-dotnet-ai-agent-benchmark/evaluator
npm install                   # once
```

## 3. Grade a bundled sample

```bash
npm run eval -- claude-opus-4-8-xhigh
```

The first run pulls Docker images (a few minutes); later runs are fast. It boots the stack, runs the
functional / Kafka / stress checks, then prints a total like `121/126 (96%)`.

## 4. Read the results

- `evaluator/results/<name>.md` - per-check breakdown (✅/❌, points, and why).
- `evaluator/results/leaderboard.md` - ranking across all submissions.

Scores are out of 126; the per-check detail explains every lost point.

## 5. Benchmark your own model

1. Paste the **entire** [`PROMPT.md`](./PROMPT.md) into the model as your message. No hints, and don't
   paste the rubric (that would teach to the test).
2. Save its output to a folder (the files it produced, with `docker-compose.yml` at the root).
3. File it as a run and grade it:

```bash
npm run add-run -- my-model ../path/to/output   # copies into submissions/my-model__run1/
npm run eval -- --leaderboard
```

Use a lowercase, dashed name (e.g. `my-model`). `add-run` skips `bin/obj/node_modules` and picks the
next run number automatically.

## 6. Do it properly: >= 5 runs per model

Models are stochastic, so one run can't rank fairly (a 1-2 point gap is just noise). Re-prompt the
model 5+ times (fresh chat each time), save each answer separately, and file them all:

```bash
npm run add-run -- my-model ../path/to/answer-1
npm run add-run -- my-model ../path/to/answer-2
# ... up to 5+ (and the same for every other model)
npm run eval                    # grade everything
npm run eval -- --leaderboard   # ranking by median, with the spread
```

The leaderboard ranks by **median total** and shows the spread (±σ, mean, range, run count). Any model
with fewer than 5 runs is flagged ⚠ as provisional.

## Useful flags

```bash
npm run eval                       # evaluate every folder in ../submissions
npm run eval -- <name>             # one submission
npm run eval -- <name> --strict-db # also verify data really persisted to Postgres
npm run eval -- --static-only      # static + architecture only (no Docker)
npm run eval -- --allow-regex-fallback  # grade without the .NET SDK (less precise)
npm test                           # the evaluator's own unit tests
```

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `docker` / `node` / `dotnet` not found | tool not installed, or terminal not reopened after installing |
| "cannot connect to the Docker daemon" | start Docker Desktop and wait for "Engine running" |
| `port 8080 / 29092 already in use` | stop whatever holds the port (or other containers) and re-run |
| first run is very slow | normal - it's pulling images; later runs are fast |
| a submission scores 0 on Build/Functional | it failed to boot (its own bug); see the `Notes` in `results/<name>.md` |
| run aborts asking for the .NET SDK | install it, or add `--allow-regex-fallback` |
