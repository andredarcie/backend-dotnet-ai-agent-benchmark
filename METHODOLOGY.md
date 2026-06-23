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

A submission is graded **as the model produced it**. The only exception so far:
`claude-sonnet-4-6-xhigh/run1` pinned `bitnami/kafka:3.7`, a tag Bitnami removed from Docker Hub, so it
could not boot at all; its Kafka service was swapped to `apache/kafka:3.9.0` (env vars translated 1:1 +
single-node `__consumer_offsets` settings) - the **.NET source was not touched**. Every other failure
below is the model's own bug, graded as-is.

## Per-run notes

### `claude-sonnet-4-6-xhigh` - 3 runs, median 47
- **run1 → 117/126** (booted, after the Kafka-image patch above).
- **run2 & run3 → 47/126 each, did not build**: both pin `Microsoft.EntityFrameworkCore 9.0.0` while
  `Npgsql.EntityFrameworkCore.PostgreSQL` (9.0.4 / 9.0.3) needs `>= 9.0.1`, so `dotnet restore` fails
  (NU1605 package downgrade) - the **same dependency bug twice**.
- All three target **.NET 9**, not 10 (-3 in Static). 2 of 3 don't compile, so the conservative median
  is **47** (±40.4, range 47-117).

### `claude-haiku-4-5` - 3 runs, median 97
- **run1 → 102/126** (booted) on **.NET 8** (-3 in Static), non-durable producer, no Kafka healthcheck.
- **run2 → 43/126, did not build**: references `Microsoft.EntityFrameworkCore.PostgreSQL`, a package that
  **does not exist** on NuGet (the real provider is `Npgsql.EntityFrameworkCore.PostgreSQL`) - a
  hallucinated dependency (NU1101).
- **run3 → 97/126** (booted, **.NET 10**, full Build/Functional/Stress), but Kafka scored only 5/20: the
  created transaction's event never reached the consumer, plus no healthcheck and a non-durable producer.
- 2 of 3 boot and the median run is a solid .NET 10 build, so the median is **97** (±32.7, range 43-102)
  - comfortably above Sonnet, whose 2 broken builds hold its median at 47.

## Takeaway

With single runs, Sonnet (run1 = 117) and Haiku (run1 = 102) looked like solid mid-pack finishers. Across
3 runs each, the real signal is **build reliability**: Sonnet ships an incompatible EF Core version 2 of 3
times, Haiku 1 of 3. That is exactly the kind of variance a one-shot ranking hides and a median-of-many
surfaces.
