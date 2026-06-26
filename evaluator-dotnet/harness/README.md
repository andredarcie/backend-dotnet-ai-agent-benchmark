# Single-command benchmark harness

Runs **all 13 criteria at once, entirely through docker-compose**. One command boots the submission
(app + Postgres + Kafka), runs OWASP ZAP (DAST) and the bundled-tool evaluator against the **live**
system on localhost, and writes the report. You just wait.

## Run

```bash
cd evaluator-dotnet/harness
# pick the submission in .env (TARGET=...), then:
docker compose up --build --abort-on-container-exit
```

When it finishes, the report is in `./results/<target>.dotnet.md` and `.json`.

## What runs, and where

| Piece | Role |
|-------|------|
| `include: submissions/<TARGET>/docker-compose.yml` | boots the system under test on host `:8080` (Kafka `:29092`) |
| `kafka-check` service (`edenhill/kcat`) | tails the `transactions` topic for the whole run → `shared/kafka.events` (key\tvalue per message) |
| `zap` service (`ghcr.io/zaproxy/zaproxy`) | OWASP ZAP baseline DAST against the live API → `shared/zap.exit` |
| `evaluator` service (this image) | Roslyn AST + **all bundled tools** + the **live contract oracle** (drives the documented credit-card/transaction flow and asserts status codes, `Location`, validation, camelCase, Problem Details, pagination); ingests ZAP + Kafka; writes the report |

The evaluator image bundles the local support tools from `EVALUATION-CRITERIA.md` so a single
container covers every criterion in `--deep` mode:

- **dotnet** (test, format, build, `list --vulnerable`) — Roslyn is in-process
- **Spectral**, **swagger-cli** (OpenAPI), **markdownlint** (docs)
- **sqlfluff** (SQL), **Schemathesis** (live contract), **Semgrep** (SAST)
- **gitleaks** (secrets), **trivy** (SCA), **hadolint** (Dockerfile), **k6** (live load)
- **OWASP ZAP** runs as its own service (DAST), ingested into category 7

Criterion → tool mapping and scoring are documented in [`../README.md`](../README.md) and the rubric in
[`../../EVALUATION-CRITERIA.md`](../../EVALUATION-CRITERIA.md).

## Configuration (`.env`)

| Var | Meaning | Default |
|-----|---------|---------|
| `TARGET` | submission folder under `../../submissions` (must contain `docker-compose.yml`) | `claude-haiku-4-5/run1` |
| `BENCH_BASE_URL` | live API as seen from tool containers | `http://host.docker.internal:8080` |
| `BENCH_OPENAPI_PATH` | path to the served OpenAPI doc | `/swagger/v1/swagger.json` |
| `BENCH_KAFKA_BOOTSTRAP` | in-network broker for the `kafka-check` consumer | `kafka:9092` |

## Notes

- **First run pulls/builds large images** (the .NET SDK + bundled tools, the ZAP image). Subsequent runs are cached.
- The submission must publish the app on host port **8080** (the benchmark convention); the tool
  containers reach it via `host.docker.internal`.
- The score still flags 🟡/🟠 categories for human review (see the rubric); the harness maximizes how
  much is measured automatically by real tools, not the human-judgment parts.
