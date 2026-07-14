# Single-command benchmark harness

Runs **all 13 criteria at once, entirely through docker-compose**. One command boots the submission
(app + Postgres + Kafka) and runs the evaluator against the **live** system on localhost, then writes
the report. You just wait.

## Run

```bash
cd evaluator-dotnet/harness
# pick the submission in .env (TARGET=...), then:
docker compose up --build --exit-code-from evaluator
```

When it finishes, the report is in `./results/<target>.dotnet.md` and `.json`.

## What runs, and where

| Piece | Role |
|-------|------|
| `include: submissions/<TARGET>/docker-compose.yml` | boots the system under test on host `:8080` (Kafka `:29092`) |
| `kafka-check` service (`edenhill/kcat`) | tails the `transactions` topic for the whole run → `shared/kafka.events` (key\tvalue per message) |
| `evaluator` service (this image) | Roslyn AST + the bundled tools + the **live contract oracle** (drives the documented credit-card/transaction flow and asserts status codes, `Location`, validation, camelCase, Problem Details, pagination) and the `/health` + `/metrics` probes; ingests `kafka.events`; writes the report |

The evaluator waits for the app itself, so it needs no `depends_on` gate; teardown is driven by **its**
completion (hence `--exit-code-from evaluator`, since `kafka-check` tails until the end).

A single container covers every criterion in `--deep` mode with a deliberately short tool list:

- **dotnet** — `build`, `test` (+ Coverlet coverage), `format`, `list package --vulnerable`; Roslyn runs in-process
- **gitleaks** (secrets), **hadolint** (Dockerfile lint)
- plus the **kcat** sidecar above and the evaluator's own **live HTTP probes / contract oracle**

> The image used to also bundle OWASP ZAP (as its own service), Semgrep, Trivy, Schemathesis, Spectral,
> swagger-cli, sqlfluff, markdownlint, lychee, dotnet-outdated, Stryker.NET, Toxiproxy and k6 (~3-4 GB).
> They were **retired**: they dominated the wall-clock and made the score depend on remote rule sets and
> CVE feeds that drift, so the same source could be graded differently on different days. The image is now
> ~1 GB. Not air-gapped, though: `dotnet restore` and the vulnerability audit behind `dotnet list package
> --vulnerable` still reach nuget.org — that one metric is time-dependent (Indeterminate when no source is
> reachable, never a silent pass). Roslyn, gitleaks and hadolint are fully offline.

Criterion → tool mapping and scoring are documented in [`../README.md`](../README.md) and the rubric in
[`../../EVALUATION-CRITERIA.md`](../../EVALUATION-CRITERIA.md).

## Configuration (`.env`)

| Var | Meaning | Default |
|-----|---------|---------|
| `TARGET` | submission folder under `../../submissions` (must contain `docker-compose.yml`) | `claude-haiku-4-5/run1` |
| `BENCH_BASE_URL` | live API as seen from the evaluator container | `http://host.docker.internal:8080` |
| `BENCH_OPENAPI_PATH` | path to the served OpenAPI doc | `/swagger/v1/swagger.json` |
| `BENCH_KAFKA_BOOTSTRAP` | in-network broker for the `kafka-check` consumer | `kafka:9092` |

## Notes

- **First run pulls/builds the images** (the .NET SDK base image + the `kcat` sidecar, ~1 GB). Subsequent runs are cached.
- The submission's suite is **unit-only** (the task rules out Testcontainers *and* `WebApplicationFactory`),
  so no Docker socket is mounted and `dotnet test` boots no sibling containers — it runs in seconds. The
  acceptance layer is the evaluator's own live contract oracle, not the submission's tests.
- The submission must publish the app on host port **8080** (the benchmark convention); the evaluator
  container reaches it via `host.docker.internal`.
- The score still flags 🟡/🟠 categories for human review (see the rubric); the harness maximizes how
  much is measured automatically by real tools, not the human-judgment parts.
