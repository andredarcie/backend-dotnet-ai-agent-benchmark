# Methodology & per-run notes

How to read the [leaderboard](./README.md#-leaderboard).

## Deterministic vs runtime scoring

- **Static** categories (Static, Architecture, Quality, and the static Kafka checks) are
  **deterministic given the Roslyn engine** - the same source always produces the same score.
- **Runtime** categories (Build/boot, Functional, runtime Kafka, Stress) depend on Docker and the host,
  so they **vary run-to-run**. Stress is the highest-variance category and is scored by the
  **conservative median across attempts**.

## Why multiple runs

Models are **stochastic**: the same prompt yields a different project each time, so a single submission
is a weak sample and a 1-2 point gap is almost certainly noise. The leaderboard groups runs per model
(`submissions/<model>/run1`, `run2`, …), ranks by the **per-model median total**, and reports the spread
(**±σ, mean, range, run count**). Models with **fewer than 5 runs are flagged ⚠ provisional**; treat
small gaps - within the spread you actually observe - as ties. See [TUTORIAL.md](./TUTORIAL.md) for how
to add runs.

## Patching policy

A submission is graded **as the model produced it**, with one narrow exception so that a purely
mechanical blocker doesn't zero out an otherwise-good project: a run that **fails to build or boot** for
a one-line reason (wrong dependency version/name, a missing package, a broken Kafka healthcheck) is
**patched minimally** so it can be scored on its merits. The fix only ever touches **dependency
declarations or compose config - never the .NET source/logic**. Each patched run carries a
`bench-patch.json` marker (`{points, reason}`) and a **-10 point penalty**, applied automatically by the
evaluator and shown in the report. Exception to the penalty: an issue that is **not the model's fault**
(a once-valid image tag removed from a registry after the model's training) is patched **without**
penalty - that is `claude-sonnet-4-6-xhigh/run1`'s `bitnami/kafka:3.7` → `apache/kafka:3.9.0` swap.

## Per-run notes

### `claude-sonnet-4-6-xhigh` - 3 runs, median 102 (±30.6)
- **run1 → 117/126** (booted; bitnami→apache image swap, **no penalty** - external registry rot).
- **run2 → 58.2/126** (patched **-10**: EF Core 9.0.0 → 9.0.4 for NU1605, + Kafka healthcheck fixed). It
  boots, but the model's own runtime is broken: Functional 2.2/25 and **strict-db FAILED** (only 1 table
  persisted) - the app starts but barely works. Graded as-is.
- **run3 → 102/126** (patched **-10**: EF Core 9.0.0 → 9.0.3 for NU1605, + Kafka healthcheck fixed).
  Boots clean, full Functional, Kafka 18/20.
- All three target **.NET 9**, not 10 (-3 in Static). Median **102**, wide spread (58-117).

### `claude-haiku-4-5` - 3 runs, median 97 (±14.9)
- **run1 → 102/126** (booted, no patch) on **.NET 8** (-3 in Static), non-durable producer, no Kafka
  healthcheck.
- **run2 → 74/126** (patched **-10**: renamed the non-existent `Microsoft.EntityFrameworkCore.PostgreSQL`
  to the real `Npgsql.EntityFrameworkCore.PostgreSQL` (NU1101), added the missing `Swashbuckle.AspNetCore`
  (CS1061), fixed the Kafka healthcheck). On **.NET 10** (Static 28/28).
- **run3 → 97/126** (booted, no patch, **.NET 10**), but Kafka only 5/20 - the created transaction's
  event never reached the consumer, plus no healthcheck and a non-durable producer.
- Median **97**, tighter spread (74-102).

## Takeaway

After fixing the mechanical build/boot blockers (and docking -10 for each), Sonnet and Haiku land
mid-pack (102 / 97) instead of dead last - which is the point: a wrong package version shouldn't bury an
otherwise-competent project. But the **reliability signal survives**: across 3 runs each, Sonnet needed a
dependency fix on 2 of 3 and Haiku on 1 of 3, Sonnet's run2 boots but doesn't actually work, and both
trail the clean single-run finishers (Gemini 108, GPT 120, Opus 121). That run-to-run variance is exactly
what a one-shot ranking hides and a median-of-many surfaces.
