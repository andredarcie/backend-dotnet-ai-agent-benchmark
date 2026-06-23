# Leaderboard

| # | Model | Runs | Total | Static+Arch | Runtime | Kafka | Quality |
|--:|-------|-----:|------:|-----------:|--------:|------:|--------:|
| 1 | `claude-opus-4-8-xhigh` | 1 ⚠ | **121/126** (96%) | 38/38 | 50/50 | 20/20 | 13/18 |
| 2 | `gpt-5-5-xhigh` | 1 ⚠ | **120/126** (95.2%) | 38/38 | 50/50 | 20/20 | 12/18 |
| 3 | `gemini-3-5-flash` | 1 ⚠ | **108/126** (85.7%) | 38/38 | 50/50 | 20/20 | 0/18 |
| 4 | `claude-sonnet-4-6-xhigh` | 3 ⚠ | **102/126** (81%) ±30.6 · mean 92.4 · 58.2-117 | 35/38 | 50/50 | 18/20 | 9/18 |
| 5 | `claude-haiku-4-5` | 3 ⚠ | **97/126** (77%) ±14.9 · mean 91 · 74-102 | 38/38 | 50/50 | 5/20 | 4/18 |

> ⚠ **Provisional ranking** - models marked ⚠ have fewer than 5 runs, so the totals are a weak sample (a one- or two-point gap is likely noise). Add more runs with `npm run add-run -- <model> <generated-project>` and re-run `npm run eval`.
