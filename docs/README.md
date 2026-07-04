# Site — Backend .NET AI Agent Benchmark

A static, **bilingual (PT/EN)** website that explains the benchmark end to end and shows the **live
leaderboard**. It's plain HTML/CSS/JS with no build step and no framework — meant to be served from this
`docs/` folder by **GitHub Pages**.

It covers, didactically and with diagrams, across two pages:

**`index.html`** — leaderboard-first:

- **Leaderboard** — the opening. Ranked by per-model median of the deep runs, with per-model
  per-category profiles on expand.
- **The 13 criteria** — an interactive rubric ladder: each category has its weight, automation level
  (🟢/🟡/🟠), a plain-language explanation, what the evaluator looks for, and a diagram.
- **How it works** — the prompt → build → evaluate → score pipeline, light vs deep mode, the Roslyn AST
  reader and the live contract oracle.

**`details.html`** — the deeper reference:

- **The task** — the credit-card API the models build (the 1:N domain, the endpoints, the Kafka event).
- **Scoring** — how a metric (Pass/Partial/Fail) becomes a category score and then the weighted 0–5.
- **Methodology** — why runs are repeated, the per-model median, and the patching policy.

## The leaderboard is dynamic

The leaderboard and per-run detail are **not hardcoded**. They're read from `data/data.js`, which
[`generate-data.ps1`](./generate-data.ps1) produces from the evaluator's reports.

**When you add or re-grade a run, just regenerate the data and the site updates itself:**

```powershell
./docs/generate-data.ps1
```

It scans `../evaluator-dotnet/results/*.dotnet.json` (one file per graded run), groups the runs per model,
computes the **median / mean / ±σ / range** the same way the .NET `LeaderboardReporter` does (deep runs
only; models with fewer than 5 runs are flagged **provisional**), and writes:

| File | What |
|------|------|
| `data/benchmark.json` | the raw, machine-readable payload |
| `data/data.js` | the same payload as `window.__BENCHMARK__ = {…};` (a plain `<script>` the page loads) |

`data.js` is loaded as a script tag (not `fetch`), so the site works over `file://` too — no server needed.

## View it locally

Just open `docs/index.html` in a browser, or serve the folder:

```powershell
# either
start docs/index.html
# or, from the repo root, any static server:
python -m http.server 8000        # then open http://localhost:8000/docs/
```

## Publish on GitHub Pages

1. Push these files.
2. In the repo: **Settings → Pages → Build and deployment → Source: "Deploy from a branch"**, branch
   `main`, folder **`/docs`**. Save.
3. The site goes live at **https://andredarcie.github.io/backend-dotnet-ai-agent-benchmark/**.

The empty `.nojekyll` file tells Pages to serve the files as-is (no Jekyll processing).

## Structure

```
docs/
├── index.html            # leaderboard + criteria + how it works
├── details.html          # the task + scoring + methodology
├── assets/
│   ├── styles.css        # the design system (light + dark themes)
│   ├── content.js        # authored bilingual copy + the 13-criteria rubric
│   └── app.js            # renders the leaderboard, rubric & diagrams; PT/EN + theme toggles
├── data/
│   ├── data.js           # GENERATED — window.__BENCHMARK__ (the leaderboard/runs)
│   └── benchmark.json    # GENERATED — the same payload as raw JSON
├── generate-data.ps1     # rebuilds data/ from evaluator-dotnet/results
└── .nojekyll
```

Both pages share `assets/` and `data/`; each renderer is guarded, so a page only renders the sections it
contains. The PT/EN and light/dark choices persist across pages (localStorage).

### What's authored vs generated

- **Authored** (`content.js`): the rubric text, diagrams and all UI copy in PT and EN. Edit here to change
  wording or explanations. The criteria facts (weight, automation level, ISO attribute) live here too, so
  the rubric renders even with zero runs.
- **Generated** (`data/*`): everything about actual runs and the ranking. Never edit by hand — re-run
  `generate-data.ps1`.
