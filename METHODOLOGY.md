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

## Graded as submitted — no patching

Every submission is scored **exactly as the model produced it**. No human (or LLM) edits the code,
config, or dependencies — there is no patching step. A build or boot blocker is not "fixed"; it is
handled by the deterministic **executability gate**, which caps the headline so a project that cannot
run cannot rank as production-grade:

- **≤ 0.5** — the source does not compile (`dotnet build` fails): the model did not even produce
  buildable code (the gravest failure).
- **≤ 1.0** — compiles but ships **no runnable system** (no `docker-compose.yml`): nothing could be
  exercised.
- **≤ 1.5** — has a compose but never boots healthy (`/health` never returned 2xx): never verified
  running.

The cap is a pure function of how far the submission got, applied by the evaluator — the same input
always yields the same cap.

## Per-run notes (current leaderboard)

- **opus-4-8 → 1.5** — the source compiles, but the Docker image fails to build (its Dockerfile
  `COPY`s `CreditCardApi.sln` while the project ships `CreditCardApi.slnx`), so the system never boots.
  Graded as submitted, it hits the **boot-fail cap (1.5)**.
- **gemini → 1.0** — the run shipped only `src/` plus a `dotnet new` test stub: **no Dockerfile, no
  docker-compose, no README**. With no runnable system it hits the **no-runnable-system cap (1.0)**.
- **haiku-4-5 → 0.5** — does not compile: 106 `CS0246`, from `[ProduceResponseType]` written without
  the "s" (53×) across every controller. It hits the **build-fail cap (0.5)**.
- **sonnet-5 (4.84), fable-5 (4.80), gpt-5-5 (4.66)** boot clean and are scored on their merits across
  all 13 categories.

## Takeaway

The executability gate is what keeps the headline honest: a clean-reading project that never runs has
demonstrated nothing, so it cannot outrank one that boots and passes the live oracle. And because every
step — the gate, the caps, the 13 category scores — is computed by the `evaluator-dotnet` tool, the
ranking is fully reproducible: same submissions in, same scores out, with **no human or LLM in the
path**.
