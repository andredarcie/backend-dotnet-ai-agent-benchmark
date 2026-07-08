# Evaluation Report — gemini/run1

- **Date (UTC):** 2026-07-06 23:28:27Z
- **Mode:** deep
- **Weighted final score:** 1.00/5
- **⚠️ Score capped:** capped at 1.0/5 — no docker-compose.yml: the submission delivers no runnable system (never exercised)
- **Coverage:** 100 % of categories
- **Executable:** build = yes · boot (/health) = n/a
- **Local tools detected:** dotnet, spectral, semgrep, trivy, gitleaks, sqlfluff, hadolint, markdownlint, lychee, k6, dotnet-outdated, swagger-cli

## Summary

| # | Category | Measure | Score | Weight |
|---|----------|---------|-------|--------|
| 1 | Functional Suitability / Correctness | 🟡 | 5.0/5 | 12% |
| 2 | Architecture & Design | 🟠 | 3.6/5 | 10% |
| 3 | Code Quality | 🟢 | 3.0/5 | 8% |
| 4 | REST API Design | 🟡 | 4.5/5 | 11% |
| 5 | Persistence & Database | 🟠 | 5.0/5 | 10% |
| 6 | Messaging | 🟢 | 5.0/5 | 11% |
| 7 | Security | 🟠 | 4.3/5 | 12% |
| 8 | Resilience & Error Handling | 🟢 | 5.0/5 | 8% |
| 9 | Tests (enabler) | 🟢 | 2.5/5 | 8% |
| 10 | Observability (enabler) | 🟢 | 5.0/5 | 4% |
| 11 | Performance & Scalability | 🟡 | 5.0/5 | 3% |
| 12 | Portability, Configuration & Deploy | 🟢 | 1.5/5 | 2% |
| 13 | Documentation | 🟠 | 2.5/5 | 1% |

## 1. Functional Suitability / Correctness 🟡

**Score:** 5.0/5 · **Weight:** 12% · **Automation:** oracle

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | test-project | yes | a test project exists (oracle suite) |
| ✅ | acceptance-blackbox | yes | black-box acceptance tests (WebApplicationFactory / Testcontainers) |
| ❔ | mutation-config | indeterminate<br/>_optional per the task — absence is not penalized_ | Stryker.NET mutation testing (optional) |
| ✅ | test-pass-rate | 1/1 passed | 100% of tests pass |

> ℹ️ SEMI: pass --base-url (the harness does) to run the live contract oracle that measures real per-endpoint correctness.

## 2. Architecture & Design 🟠

**Score:** 3.6/5 · **Weight:** 10% · **Automation:** proxy

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | layering | yes | layer separation (domain/infra/presentation) |
| ❌ | application-layer | no | application/use-case layer present |
| ✅ | dependency-direction | 0 leaks | domain does not reference infrastructure (Roslyn usings) |
| ❔ | overengineering-proxy | indeterminate<br/>_5/5 single-implementation interfaces — normal DIP ports (repository/UoW/publisher); informational, not scored_ | few speculative abstractions |
| ✅ | no-god-class | largest type: 207 lines | no 'god classes' (<=600 lines) |

> ℹ️ PROXY: layering and overengineering are scored automatically from Roslyn dependency-direction, class-size and single-implementation-interface metrics. NDepend/SonarQube can deepen this.

## 3. Code Quality 🟢

**Score:** 3.0/5 · **Weight:** 8% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ❌ | no-empty-catch | 2 | no empty catch (no swallowed exceptions) |
| ✅ | no-todos | 0 | no pending TODO/FIXME/HACK |
| ✅ | analyzers-enabled | yes | analyzers/.editorconfig enabled |
| ❌ | format | formatting diverges | code is formatted (dotnet format) |
| ✅ | build-warnings | 0 warning(s) | 0 build warnings |


## 4. REST API Design 🟡

**Score:** 4.5/5 · **Weight:** 11% · **Automation:** oracle

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | http-verbs | yes | correct HTTP verbs (Richardson L2) |
| ✅ | status-codes | yes | explicit, coherent status codes |
| ✅ | problem-details | yes | standardized errors RFC 9457 (ProblemDetails) |
| ✅ | openapi | yes | OpenAPI/Swagger contract exposed |
| ❌ | versioning | no | API versioning |
| ✅ | dtos | yes | separates DTOs from domain entities |

> ℹ️ No static OpenAPI file found (spec is likely generated at runtime); run --deep to generate and lint it.
> ℹ️ SEMI: pass --base-url (the harness does) to assert live status codes / Location / Problem Details / pagination.

## 5. Persistence & Database 🟠

**Score:** 5.0/5 · **Weight:** 10% · **Automation:** proxy

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | migrations | versioned migrations | schema evolves via migrations (not EnsureCreated) |
| ✅ | referential-integrity | yes | referential integrity (FK/relationships) |
| ✅ | indexes | yes | indexes defined (incl. FKs/queries) |
| ✅ | concurrency | yes | concurrency control (optimistic) |
| ✅ | read-perf | yes | AsNoTracking on reads (efficiency proxy) |

> ℹ️ PROXY: schema shape (3NF heuristics, FKs, indexes, concurrency) is scored automatically from Roslyn; N+1 / seq-scan signals use the live DB in --deep (pg_stat_statements/EXPLAIN).
> ⚠️ Missing tools: schemacrawler

## 6. Messaging 🟢

**Score:** 5.0/5 · **Weight:** 11% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | broker-client | yes | messaging client present |
| ✅ | durable-producer | yes | durable producer (Acks.All / idempotence) |
| ✅ | idempotent-consumer | yes | idempotent consumer (dedupe by id) |
| ✅ | outbox | yes | Transactional Outbox (DB<->broker consistency) |
| ✅ | dlq | yes | dead-letter queue for failures |
| ✅ | offset-after-process | yes | commit offset after processing (auto-commit off) |
| ✅ | messaging-tests | yes | messaging integration tests (Testcontainers-Kafka) |

> ℹ️ Deep messaging checks (publish-duplicate -> single effect; kill consumer mid-process; Schema Registry compatibility) require a Kafka container and the app running.

## 7. Security 🟠

**Score:** 4.3/5 · **Weight:** 12% · **Automation:** proxy

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | pci-pan | 0 | no PAN (Luhn-valid card) embedded in code/config |
| ✅ | pci-sad | absent | no sensitive auth data stored (CVV/track/PIN) |
| ❔ | authz | indeterminate<br/>_out of scope — absence is not a finding_ | authentication/authorization (optional — not scored) |
| ✅ | validation | yes | input validation |
| ✅ | rate-limit | yes | rate limiting (OWASP API #4) |
| ✅ | tls | HSTS configured | TLS/HSTS configured for production |
| ❌ | secrets | leaks found<br/>_review gitleaks findings_ | no hardcoded secrets (gitleaks) |
| ✅ | sca | none reported | 0 High/Critical dependencies |
| ✅ | sca-trivy | none High/Critical | 0 High/Critical vulnerabilities (Trivy) |
| ✅ | sast | clean | no SAST findings (Semgrep) |

> ℹ️ PROXY: scored automatically from SAST/DAST tool output and the live BOLA oracle scenario (user A vs resource of B) in --deep.

## 8. Resilience & Error Handling 🟢

**Score:** 5.0/5 · **Weight:** 8% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | resilience-policies | yes | retry/timeout/circuit-breaker policies (Polly) |
| ✅ | health-checks | yes | health checks (liveness/readiness) |
| ✅ | global-error-handling | yes | global exception handling (no stack-trace leak) |
| ✅ | graceful-shutdown | yes | graceful shutdown / hosted services |
| ✅ | timeouts | yes | timeouts / cancellation propagated |

> ℹ️ Real fault injection (Toxiproxy) and recovery measurement need --deep + a running container.
> ⚠️ Missing tools: toxiproxy-cli

## 9. Tests (enabler) 🟢

**Score:** 2.5/5 · **Weight:** 8% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | test-framework | yes | test project(s) with a framework |
| ✅ | pyramid | unit=False, integration=True | pyramid: unit + integration |
| ✅ | coverage-tool | yes | coverage tool (Coverlet) |
| ❔ | mutation-tool | indeterminate<br/>_optional per the task — absence is not penalized_ | mutation testing (Stryker.NET, optional) |
| ❌ | coverage | 0 % (1 report(s) merged) | line coverage >=80% |


## 10. Observability (enabler) 🟢

**Score:** 5.0/5 · **Weight:** 4% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | otel | yes | OpenTelemetry (traces/metrics/logs) |
| ✅ | structured-logs | yes | structured logging (JSON / Serilog) |
| ✅ | metrics-endpoint | yes | metrics exposed (Prometheus / Meter) |
| ✅ | correlation | yes | request correlation (trace/correlation id) |
| ✅ | health-endpoint | yes | health/diagnostics endpoint |


## 11. Performance & Scalability 🟡

**Score:** 5.0/5 · **Weight:** 3% · **Automation:** oracle

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | async-io | 31 async methods | asynchronous/non-blocking I/O |
| ✅ | no-sync-over-async | none | no sync-over-async blocking (.Wait/.GetResult) |
| ✅ | stateless | no obvious in-memory state | stateless API (horizontal scaling) |
| ✅ | pagination | yes | pagination on collections (scaling proxy) |

> ℹ️ SEMI: the latency/throughput score needs a target SLO (oracle) and a load test against the running API (k6, set up by the docker-compose harness).

## 12. Portability, Configuration & Deploy 🟢

**Score:** 1.5/5 · **Weight:** 2% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ❌ | dockerfile | no | Dockerfile present |
| ❌ | compose | no | docker-compose for dependencies |
| ✅ | env-config | yes | externalized config (env vars, 12-Factor III/IV) |
| ❌ | pinning | no | pinned dependencies (lock file / global.json / Central Package Management) |
| ❌ | ci | no | CI pipeline present |
| ❌ | non-root | no | container runs as non-root |
| ✅ | outdated | checked | dependencies reasonably up to date |


## 13. Documentation 🟠

**Score:** 2.5/5 · **Weight:** 1% · **Automation:** proxy

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ❌ | readme | no | README present |
| ✅ | api-docs | yes | API documentation (OpenAPI/Swagger) |
| ✅ | doc-comments | yes | doc comments / XML docs |

> ℹ️ PROXY: README section/link presence, OpenAPI completeness and doc-comment coverage are scored automatically.

