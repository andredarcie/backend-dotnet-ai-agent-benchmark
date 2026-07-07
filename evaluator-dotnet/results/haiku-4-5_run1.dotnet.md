# Evaluation Report — haiku-4-5/run1

- **Date (UTC):** 2026-07-06 22:26:12Z
- **Mode:** deep
- **Weighted final score:** 0.50/5
- **⚠️ Score capped:** capped at 0.5/5 — source does not compile (dotnet build failed)
- **Coverage:** 100 % of categories
- **Executable:** build = **NO** · boot (/health) = n/a
- **Local tools detected:** dotnet, spectral, semgrep, trivy, gitleaks, sqlfluff, hadolint, markdownlint, lychee, k6, dotnet-outdated, swagger-cli

## Summary

| # | Category | Auto | Score | Weight | Review |
|---|----------|------|-------|--------|--------|
| 1 | Functional Suitability / Correctness | 🟡 | 5.0/5 | 12% | yes |
| 2 | Architecture & Design | 🟠 | 5.0/5 | 10% | yes |
| 3 | Code Quality | 🟢 | 3.8/5 | 8% | — |
| 4 | REST API Design | 🟡 | 4.5/5 | 11% | yes |
| 5 | Persistence & Database | 🟠 | 4.3/5 | 10% | yes |
| 6 | Messaging | 🟢 | 2.3/5 | 11% | — |
| 7 | Security | 🟠 | 3.6/5 | 12% | yes |
| 8 | Resilience & Error Handling | 🟢 | 1.9/5 | 8% | — |
| 9 | Tests (enabler) | 🟢 | 5.0/5 | 8% | — |
| 10 | Observability (enabler) | 🟢 | 2.9/5 | 4% | — |
| 11 | Performance & Scalability | 🟡 | 5.0/5 | 3% | yes |
| 12 | Portability, Configuration & Deploy | 🟢 | 4.3/5 | 2% | — |
| 13 | Documentation | 🟠 | 4.7/5 | 1% | yes |

## 1. Functional Suitability / Correctness 🟡

**Score:** 5.0/5 · **Weight:** 12% · **Automation:** semi (oracle 1x)

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | test-project | yes | a test project exists (oracle suite) |
| ✅ | acceptance-blackbox | yes | black-box acceptance tests (WebApplicationFactory / Testcontainers) |
| ❔ | mutation-config | indeterminate<br/>_optional per the task — absence is not penalized_ | Stryker.NET mutation testing (optional) |
| ❔ | test-pass-rate | indeterminate<br/>_could not parse `dotnet test` output_ | 100% of tests pass |

> ℹ️ SEMI: pass --base-url (the harness does) to run the live contract oracle that measures real per-endpoint correctness.

## 2. Architecture & Design 🟠

**Score:** 5.0/5 · **Weight:** 10% · **Automation:** proxy + review

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | layering | yes | layer separation (domain/infra/presentation) |
| ✅ | application-layer | yes | application/use-case layer present |
| ✅ | dependency-direction | 0 leaks | domain does not reference infrastructure (Roslyn usings) |
| ❔ | overengineering-proxy | indeterminate<br/>_3/3 single-implementation interfaces — normal DIP ports (repository/UoW/publisher); flagged for human review, not scored_ | few speculative abstractions |
| ✅ | no-god-class | largest type: 267 lines | no 'god classes' (<=600 lines) |

> ℹ️ PROXY: layering needs layer rules (oracle) and the overengineering verdict needs human review. NDepend/SonarQube can deepen this.

## 3. Code Quality 🟢

**Score:** 3.8/5 · **Weight:** 8% · **Automation:** full-auto

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | no-empty-catch | 0 | no empty catch (no swallowed exceptions) |
| ✅ | no-todos | 0 | no pending TODO/FIXME/HACK |
| ✅ | analyzers-enabled | yes | analyzers/.editorconfig enabled |
| ❌ | format | formatting diverges | code is formatted (dotnet format) |
| ❔ | build-warnings | indeterminate<br/>_build summary unavailable_ | 0 build warnings |


## 4. REST API Design 🟡

**Score:** 4.5/5 · **Weight:** 11% · **Automation:** semi (oracle 1x)

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

**Score:** 4.3/5 · **Weight:** 10% · **Automation:** proxy + review

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| 🟨 | migrations | uses EnsureCreated | schema evolves via migrations (not EnsureCreated) |
| ✅ | referential-integrity | yes | referential integrity (FK/relationships) |
| ✅ | indexes | yes | indexes defined (incl. FKs/queries) |
| ✅ | concurrency | yes | concurrency control (optimistic) |
| ✅ | read-perf | yes | AsNoTracking on reads (efficiency proxy) |

> ℹ️ PROXY: 3NF / justified denormalization need functional-dependency analysis (human review). N+1, seq scans and EXPLAIN need a live DB (--deep + container, e.g. via pg_stat_statements/HypoPG).
> ⚠️ Missing tools: schemacrawler

## 6. Messaging 🟢

**Score:** 2.3/5 · **Weight:** 11% · **Automation:** full-auto

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | broker-client | yes | messaging client present |
| ✅ | durable-producer | yes | durable producer (Acks.All / idempotence) |
| ❌ | idempotent-consumer | no | idempotent consumer (dedupe by id) |
| ❌ | outbox | no | Transactional Outbox (DB<->broker consistency) |
| ❌ | dlq | no | dead-letter queue for failures |
| ❌ | offset-after-process | no | commit offset after processing (auto-commit off) |
| ✅ | messaging-tests | yes | messaging integration tests (Testcontainers-Kafka) |

> ℹ️ Deep messaging checks (publish-duplicate -> single effect; kill consumer mid-process; Schema Registry compatibility) require a Kafka container and the app running.

## 7. Security 🟠

**Score:** 3.6/5 · **Weight:** 12% · **Automation:** proxy + review

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | pci-pan | 0 | no PAN (Luhn-valid card) embedded in code/config |
| ✅ | pci-sad | absent | no sensitive auth data stored (CVV/track/PIN) |
| ❔ | authz | indeterminate<br/>_out of scope — absence is not a finding_ | authentication/authorization (optional — not scored) |
| ❌ | validation | no | input validation |
| ❌ | rate-limit | no | rate limiting (OWASP API #4) |
| ❌ | tls | none | TLS/HSTS configured for production |
| ✅ | secrets | 0 leaks | no hardcoded secrets (gitleaks) |
| ✅ | sca | none reported | 0 High/Critical dependencies |
| ✅ | sca-trivy | none High/Critical | 0 High/Critical vulnerabilities (Trivy) |
| 🟨 | sast | findings (triage needed) | no SAST findings (Semgrep) |

> ℹ️ PROXY: SAST/DAST findings need false-positive triage and the BOLA test needs an oracle scenario (user A vs resource of B). Final verdict = human review.

## 8. Resilience & Error Handling 🟢

**Score:** 1.9/5 · **Weight:** 8% · **Automation:** full-auto

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | resilience-policies | yes | retry/timeout/circuit-breaker policies (Polly) |
| ❌ | health-checks | no | health checks (liveness/readiness) |
| ❌ | global-error-handling | no | global exception handling (no stack-trace leak) |
| ❌ | graceful-shutdown | no | graceful shutdown / hosted services |
| ✅ | timeouts | yes | timeouts / cancellation propagated |

> ℹ️ Real fault injection (Toxiproxy) and recovery measurement need --deep + a running container.
> ⚠️ Missing tools: toxiproxy-cli

## 9. Tests (enabler) 🟢

**Score:** 5.0/5 · **Weight:** 8% · **Automation:** full-auto

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | test-framework | yes | test project(s) with a framework |
| ✅ | pyramid | unit=False, integration=True | pyramid: unit + integration |
| ✅ | coverage-tool | yes | coverage tool (Coverlet) |
| ❔ | mutation-tool | indeterminate<br/>_optional per the task — absence is not penalized_ | mutation testing (Stryker.NET, optional) |
| ❔ | coverage | indeterminate<br/>_no coverage.cobertura.xml produced_ | line coverage >=80% |


## 10. Observability (enabler) 🟢

**Score:** 2.9/5 · **Weight:** 4% · **Automation:** full-auto

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | otel | yes | OpenTelemetry (traces/metrics/logs) |
| ✅ | structured-logs | yes | structured logging (JSON / Serilog) |
| ❌ | metrics-endpoint | no | metrics exposed (Prometheus / Meter) |
| ❌ | correlation | no | request correlation (trace/correlation id) |
| ❌ | health-endpoint | no | health/diagnostics endpoint |


## 11. Performance & Scalability 🟡

**Score:** 5.0/5 · **Weight:** 3% · **Automation:** semi (oracle 1x)

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | async-io | 39 async methods | asynchronous/non-blocking I/O |
| ✅ | no-sync-over-async | none | no sync-over-async blocking (.Wait/.GetResult) |
| ✅ | stateless | no obvious in-memory state | stateless API (horizontal scaling) |
| ✅ | pagination | yes | pagination on collections (scaling proxy) |

> ℹ️ SEMI: the latency/throughput score needs a target SLO (oracle) and a load test against the running API (k6, set up by the docker-compose harness).

## 12. Portability, Configuration & Deploy 🟢

**Score:** 4.3/5 · **Weight:** 2% · **Automation:** full-auto

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | dockerfile | yes | Dockerfile present |
| ✅ | compose | yes | docker-compose for dependencies |
| ✅ | env-config | yes | externalized config (env vars, 12-Factor III/IV) |
| ✅ | pinning | yes | pinned dependencies (lock file / global.json / Central Package Management) |
| ❌ | ci | no | CI pipeline present |
| ✅ | non-root | yes | container runs as non-root |
| ✅ | hadolint | clean | Dockerfile with no violations (hadolint) |
| 🟨 | outdated | outdated packages found | dependencies reasonably up to date |


## 13. Documentation 🟠

**Score:** 4.7/5 · **Weight:** 1% · **Automation:** proxy + review

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | readme | yes | README present |
| ✅ | readme-sections | 3/3 | README with purpose+setup+run |
| ✅ | api-docs | yes | API documentation (OpenAPI/Swagger) |
| ✅ | doc-comments | yes | doc comments / XML docs |
| 🟨 | markdownlint | violations | README passes markdownlint |
| ✅ | links | no broken links | no broken links (lychee) |

> ℹ️ PROXY: section/link presence is automatic, but the QUALITY of the prose needs human review.

