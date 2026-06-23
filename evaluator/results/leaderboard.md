# Leaderboard

| # | Model | Runs | Total | Static | Arch | Boot | Functional | Kafka | Stress | Quality |
|--:|-------|-----:|------:|-------:|-----:|-----:|-----------:|------:|-------:|--------:|
| 1 | `claude-opus-4-8-xhigh` | 1 ⚠ | **121/126** (96%) | 28/28 | 10/10 | 15/15 | 25/25 | 20/20 | 10/10 | 13/18 |
| 2 | `gpt-5-5-xhigh` | 1 ⚠ | **120/126** (95.2%) | 28/28 | 10/10 | 15/15 | 25/25 | 20/20 | 10/10 | 12/18 |
| 3 | `gemini-3-5-flash` | 1 ⚠ | **108/126** (85.7%) | 28/28 | 10/10 | 15/15 | 25/25 | 20/20 | 10/10 | 0/18 |
| 4 | `claude-haiku-4-5` | 1 ⚠ | **102/126** (81%) | 25/28 | 10/10 | 15/15 | 25/25 | 15/20 | 10/10 | 2/18 |
| 5 | `claude-sonnet-4-6-xhigh` | 3 ⚠ | **47/126** (37.3%) ±40.4 · mean 70.3 · 47-117 | 25/28 | 10/10 | 0/15 | 0/25 | 3/20 | 0/10 | 9/18 |

> ⚠ **Provisional ranking** - models marked ⚠ have fewer than 5 runs, so the totals are a weak sample (a one- or two-point gap is likely noise). Add more runs with `npm run add-run -- <model> <generated-project>` and re-run `npm run eval`.
