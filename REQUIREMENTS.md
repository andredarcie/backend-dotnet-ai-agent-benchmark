# Scoring rubric

The evaluator scores every submission across **7 categories**, for a total of **126 points**.
Each check is pass/fail (or graded, for stress) and carries a weight. The exact weights live
in `evaluator/src/config.ts` - this document mirrors them for transparency.

| Category                  | Weight | What it measures                                          |
|---------------------------|-------:|-----------------------------------------------------------|
| 1. Static requirements    | 28     | The code contains what the prompt asked (incl. **.NET 10 target**) |
| 2. Architecture (layering)| 10     | Controller → Use Case → Repository is actually wired      |
| 3. Build & boot           | 15     | `docker compose up` works and the API comes alive         |
| 4. Functional behavior    | 25     | Endpoints behave per the contract                         |
| 5. Kafka integration      | 20     | Publishes to the topic; broker **healthcheck**; **durable** producer |
| 6. Stress / load          | 10     | Holds up under concurrent traffic                         |
| 7. Best practices (quality)| 18    | Engineering quality beyond the minimum (tie-breakers)     |

Categories 1, 2 and 7 (plus the static part of 5) are static (no Docker). Categories 3, 4, 6 and
the runtime part of 5 require a running container; if boot fails they score 0.

Static analysis runs with **Roslyn by default**; the report records which engine produced each
static row, and `--allow-regex-fallback` permits the regex heuristics if the .NET SDK is absent.

---

## 1. Static requirements (28 pts)

Source-code analysis - no Docker needed. C# structure is analyzed with **Roslyn** (real syntax
trees - understands primary constructors, attributes, comments, partial classes), falling back
to regex heuristics only if the .NET SDK is unavailable. `docker-compose`/`Dockerfile` checks are
file-based. (`bin/`, `obj/`, `node_modules/`, `.git/` are skipped.) Each report row is tagged
`[roslyn]` or `[regex]` so you can see which engine produced it.

| Check                         | Pts | How it is detected                                                       |
|-------------------------------|----:|--------------------------------------------------------------------------|
| `docker-compose.yml` present  | 2   | A `docker-compose.y(a)ml` exists at the submission root                  |
| Compose uses Postgres         | 2   | A service in the compose uses a `postgres` image                         |
| Compose builds the API        | 2   | A compose service has a `build:` (the API image)                         |
| Dockerfile present            | 2   | A `Dockerfile` exists                                                    |
| Compose has Kafka             | 2   | A service uses a `kafka` image (or is named `kafka`)                     |
| Two controllers               | 3   | ≥ 2 classes that are `[ApiController]` or inherit `ControllerBase`       |
| Two entities                  | 3   | ≥ 2 `DbSet<>` whose type arg is an actual declared class (ignores generics like `DbSet<TEntity>`) |
| Uses an ORM (EF Core)         | 3   | `Microsoft.EntityFrameworkCore` referenced AND a `DbContext` subclass    |
| Uses the Postgres provider    | 2   | A `UseNpgsql(...)` **wiring call** (a package reference alone is not enough) |
| Models a 1:N relationship     | 2   | A foreign-key signal: `HasForeignKey`, `[ForeignKey]`, or a `…Id` + nav property |
| Kafka client + publish        | 2   | A Kafka client reference (`Confluent.Kafka`) AND a produce call (`ProduceAsync` / `Produce` / `IProducer`) |
| **Targets .NET 10**           | 3   | `TargetFramework` is `net10.*` - wrong .NET version is a contract violation, but it is weighted as a single contract item (not heavier than core structural checks) |

## 2. Architecture / layering (10 pts)

Verifies the required call chain **Controller → Use Case → Repository → EF Core**.

| Check                          | Pts | How it is detected                                                         |
|--------------------------------|----:|----------------------------------------------------------------------------|
| Repository layer present       | 2   | A `*Repository` interface AND a concrete implementation                    |
| Base repository class present  | 2   | An abstract/generic base repository (e.g. `RepositoryBase<T>`) that repos extend |
| Use-case layer present         | 3   | ≥ 1 `*UseCase` (or `*Interactor`) class                                    |
| Controllers call use cases     | 2   | Controller files reference a use case **and do not** use `DbContext` directly |
| Repositories own EF Core       | 1   | Repository files reference `DbContext` / EF Core (data access isolated there) |

## 3. Build & boot (15 pts)

| Check                | Pts | How it is detected                                              |
|----------------------|----:|-----------------------------------------------------------------|
| `docker compose up`  | 8   | The compose comes up without the runner aborting                |
| API becomes healthy  | 7   | `GET /health` returns 200 (or `/api/credit-cards` returns < 500) within the boot timeout |

If boot fails, categories 4, 5 and 6 are skipped and score 0.

## 4. Functional behavior (25 pts)

Live HTTP tests against the contract in [`PROMPT.md`](./PROMPT.md). Each assertion is worth points:

- Health endpoint returns 200.
- **Credit cards:** create (201 + `id` + **`Location` header** + **all fields echoed in camelCase**),
  get by id (200), list (200 array), update, delete (204), get-after-delete (404), get-missing (404),
  validation (empty `cardholderName` → 400).
- **Transactions:** create against a valid card (201 + `id` + `Location` + all fields echoed),
  get by id (200), list (200 array), update, delete (204), validation (`amount <= 0` → 400;
  non-existent `creditCardId` → 400).
- **Relationship:** `GET /api/credit-cards/{id}/transactions` returns that card's transactions (200),
  and 404 for a missing card.

The 25 points are split evenly across the assertions actually run.

## 5. Kafka integration (20 pts)

The evaluator runs on the host and connects a Kafka consumer to `localhost:29092`, subscribed to
the `transactions` topic. It then creates a credit card and a transaction over HTTP and waits for
the corresponding event. Two of the checks are static (compose / producer config).

| Check                          | Pts | Pass condition                                                       |
|--------------------------------|----:|----------------------------------------------------------------------|
| Broker reachable from host     | 5   | A consumer connects to `localhost:29092` and subscribes to `transactions` |
| Transaction event published (value) | 8 | A message referencing the just-created transaction (`id` / `merchant`) arrives within the timeout |
| Event message key = transaction id | 2 | The produced message **key** equals the transaction id, as required by PROMPT §4 |
| Kafka healthcheck (compose)    | 3   | The compose Kafka service defines a healthcheck (static) |
| Durable producer               | 2   | Producer configured for durability - `Acks.All` / `EnableIdempotence` (static) |

If the broker is not exposed on `localhost:29092`, the two runtime checks fail but the two static
Kafka checks (healthcheck, durability) and the rest of the run continue.

## 6. Stress / load (10 pts)

A self-contained load generator drives concurrent traffic (configurable, default 50 concurrent
workers for 15s) mixing reads and writes.

| Check              | Pts | Pass condition                                          |
|--------------------|----:|---------------------------------------------------------|
| Error rate         | 6   | < **1%** of requests fail (5xx / connection errors)     |
| Throughput         | 2   | sustained **≥ 50 req/s** (a real API does hundreds)     |
| Latency p95        | 2   | p95 < **1000 ms**                                       |

The report also records raw metrics (total requests, RPS, p50/p95/p99 latency) for comparison
even when thresholds pass. Thresholds are tunable via env vars (`BENCH_STRESS_MAX_ERR`,
`BENCH_STRESS_MIN_RPS`, `BENCH_STRESS_MAX_P95`).

**Honest caveat:** these thresholds are a **correctness floor** - most working APIs pass them, so
this category rarely discriminates between healthy submissions. To avoid optimistic bias, the
recorded score uses the **conservative MEDIAN across attempts** (NOT best-of-N / max).

## 7. Best practices (quality) (18 pts)

Engineering quality **beyond the minimum** - these are the tie-breakers that separate otherwise
perfect submissions. All static (Roslyn + compose + Dockerfile).

| Check                       | Pts | Pass condition                                                       |
|-----------------------------|----:|----------------------------------------------------------------------|
| No hardcoded `container_name` | 2 | Compose has no `container_name:` (lets the stack be isolated/run in parallel) |
| Kafka in KRaft mode         | 2   | No Zookeeper service in the compose                                  |
| Up-to-date Kafka image      | 1   | Recent Kafka image tag (e.g. cp-kafka ≥ 7.6 / apache-kafka ≥ 3.8)    |
| CancellationToken propagated| 3   | Controllers **and** repositories take a `CancellationToken`         |
| Response DTOs               | 2   | Returns DTOs (`*Response`/`*Dto`), doesn't leak EF entities          |
| Structured error handling   | 2   | `IExceptionHandler` / `AddProblemDetails` / a `Result` pattern       |
| EF migrations               | 2   | Production-grade schema management - a `Migrations/` folder or `Database.Migrate()`. A **bonus** for managed migrations over `EnsureCreated` (not a penalty for following the prompt) |
| Non-root container          | 1   | Dockerfile sets a non-root `USER`                                    |
| Resilient Kafka publish     | 3   | Publish failure handled gracefully (catch-and-log **OR** a transactional outbox), not propagated as a 500 - a broker hiccup doesn't fail the request after the data is persisted |

---

## Weighting rationale

Why the categories carry the weights they do:

- **Functional behavior (25)** is the highest single weight: contract conformance is the core
  deliverable - an API that doesn't behave per the spec has failed regardless of how it's built.
- **Kafka (20)** reflects that event publishing is a first-class *required* integration, verified
  at runtime (broker reachability, value, and key), not an optional extra.
- **Static requirements (28)** confirm the required stack/structure cheaply and deterministically;
  many small, unambiguous checks add up, but no single one (including `.NET 10`) dominates.
- **Build & boot (15)** gates everything downstream - if the stack doesn't come up, the functional,
  Kafka-runtime, and stress categories can't even be measured.
- **Architecture (10)** and **quality (18)** are tie-breakers that separate otherwise-passing
  submissions on engineering rigor (layering, DTOs, resilience, structured errors).
- **Stress (10)** is a correctness floor with the highest run-to-run variance, so it is deliberately
  *not* over-weighted; its score uses the conservative median across attempts.

---

## Output

For each submission the evaluator writes:

- `evaluator/results/<name>.json` - full machine-readable report.
- `evaluator/results/<name>.md` - human-readable breakdown.

And across all submissions:

- `evaluator/results/leaderboard.md` - ranked comparison table.
