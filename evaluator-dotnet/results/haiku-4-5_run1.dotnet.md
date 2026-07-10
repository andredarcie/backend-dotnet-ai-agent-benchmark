# Evaluation Report — haiku-4-5/run1

- **Date (UTC):** 2026-07-10 13:58:00Z
- **Mode:** deep
- **Weighted final score:** 1.50/5
- **⚠️ Score capped:** capped at 1.5/5 — the system was never verified running (/health never returned 2xx); a deep score requires a live boot
- **Coverage:** 100 % of categories
- **Executable:** build = yes · boot (/health) = **NO**
- **Local tools detected:** dotnet, docker, k6, reportgenerator

## Summary

| # | Category | Measure | Score | Weight |
|---|----------|---------|-------|--------|
| 1 | Functional Suitability / Correctness | 🟡 | 2.5/5 | 12% |
| 2 | Architecture & Design | 🟠 | 5.0/5 | 10% |
| 3 | Code Quality | 🟢 | 5.0/5 | 8% |
| 4 | REST API Design | 🟡 | 4.5/5 | 11% |
| 5 | Persistence & Database | 🟠 | 5.0/5 | 10% |
| 6 | Messaging | 🟢 | 2.3/5 | 11% |
| 7 | Security | 🟠 | 2.2/5 | 12% |
| 8 | Resilience & Error Handling | 🟢 | 5.0/5 | 8% |
| 9 | Tests (enabler) | 🟢 | 2.5/5 | 8% |
| 10 | Observability (enabler) | 🟢 | 1.5/5 | 4% |
| 11 | Performance & Scalability | 🟡 | 3.9/5 | 3% |
| 12 | Portability, Configuration & Deploy | 🟢 | 4.4/5 | 2% |
| 13 | Documentation | 🟠 | 4.2/5 | 1% |

## 1. Functional Suitability / Correctness 🟡

**Score:** 2.5/5 · **Weight:** 12% · **Automation:** oracle

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | test-project | yes | a test project exists (oracle suite) |
| ✅ | acceptance-blackbox | yes | black-box acceptance tests (WebApplicationFactory / Testcontainers) |
| ❔ | mutation-config | indeterminate<br/>_optional per the task — absence is not penalized_ | Stryker.NET mutation testing (optional) |
| ❔ | schemathesis | indeterminate<br/>_no OpenAPI document served (tried /openapi/v1.json, /swagger/v1/swagger.json, …) — not scored_ | API conforms to its OpenAPI contract (Schemathesis) |
| ❌ | test-pass-rate | 4/17 passed | 100% of tests pass |

> ℹ️ SEMI: pass --base-url (the harness does) to run the live contract oracle that measures real per-endpoint correctness.

## 2. Architecture & Design 🟠

**Score:** 5.0/5 · **Weight:** 10% · **Automation:** proxy

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | layering | yes | layer separation (domain/infra/presentation) |
| ✅ | application-layer | yes | application/use-case layer present |
| ✅ | dependency-direction | 0 leaks | domain does not reference infrastructure (Roslyn usings) |
| ❔ | overengineering-proxy | indeterminate<br/>_1/1 single-implementation interfaces — normal DIP ports (repository/UoW/publisher); informational, not scored_ | few speculative abstractions |
| ✅ | no-god-class | largest type: 358 lines | no 'god classes' (<=600 lines) |

> ℹ️ PROXY: layering and overengineering are scored automatically from Roslyn dependency-direction, class-size and single-implementation-interface metrics. NDepend/SonarQube can deepen this.

## 3. Code Quality 🟢

**Score:** 5.0/5 · **Weight:** 8% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | no-empty-catch | 0 | no empty catch (no swallowed exceptions) |
| ✅ | no-todos | 0 | no pending TODO/FIXME/HACK |
| ✅ | analyzers-enabled | yes | analyzers/.editorconfig enabled |
| ✅ | format | no changes | code is formatted (dotnet format) |
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
| ❔ | openapi-populated | indeterminate<br/>_no OpenAPI document served — not scored_ | served OpenAPI documents its operations |

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

**Score:** 2.3/5 · **Weight:** 11% · **Automation:** deterministic

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

**Score:** 2.2/5 · **Weight:** 12% · **Automation:** proxy

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | pci-pan | 0 | no PAN (Luhn-valid card) embedded in code/config |
| ✅ | pci-sad | absent | no sensitive auth data stored (CVV/track/PIN) |
| ❔ | authz | indeterminate<br/>_out of scope — absence is not a finding_ | authentication/authorization (optional — not scored) |
| ❌ | validation | no | input validation |
| ❌ | rate-limit | no | rate limiting (OWASP API #4) |
| ❌ | tls | none | TLS/HSTS configured for production |
| ❔ | secrets | indeterminate<br/>_tool 'gitleaks' not installed — install: https://github.com/gitleaks/gitleaks_ | no hardcoded secrets (gitleaks) |
| ❌ | sca | vulnerable packages | 0 High/Critical dependencies |
| ❔ | sca-trivy | indeterminate<br/>_tool 'trivy' not installed — install: https://aquasecurity.github.io/trivy/_ | 0 High/Critical vulnerabilities (Trivy) |
| ❔ | sast | indeterminate<br/>_tool 'semgrep' not installed — install: pip install semgrep_ | no SAST findings (Semgrep) |

> ℹ️ PROXY: scored automatically from SAST/DAST tool output and the live BOLA oracle scenario (user A vs resource of B) in --deep.
> ⚠️ Missing tools: gitleaks, trivy, semgrep

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
| ❌ | coverage | 19 % (1 report(s) merged) | line coverage >=80% |


## 10. Observability (enabler) 🟢

**Score:** 1.5/5 · **Weight:** 4% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ❌ | otel | no | OpenTelemetry (traces/metrics/logs) |
| ✅ | structured-logs | yes | structured logging (JSON / Serilog) |
| ❌ | metrics-endpoint | no | metrics exposed (Prometheus / Meter) |
| ❌ | correlation | no | request correlation (trace/correlation id) |
| ✅ | health-endpoint | yes | health/diagnostics endpoint |
| ❌ | live-health | unreachable | /health responds 2xx live |
| ❌ | live-metrics | unreachable | /metrics responds live |


## 11. Performance & Scalability 🟡

**Score:** 3.9/5 · **Weight:** 3% · **Automation:** oracle

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | async-io | 34 async methods | asynchronous/non-blocking I/O |
| ✅ | no-sync-over-async | none | no sync-over-async blocking (.Wait/.GetResult) |
| ✅ | stateless | no obvious in-memory state | stateless API (horizontal scaling) |
| ✅ | pagination | yes | pagination on collections (scaling proxy) |
| ❌ | concurrency | 0 5xx, 60 failed (of 60) | survives concurrent load (no 5xx / hangs) |

> ℹ️ SEMI: the latency/throughput score needs a target SLO (oracle) and a load test against the running API (k6, set up by the docker-compose harness).

## 12. Portability, Configuration & Deploy 🟢

**Score:** 4.4/5 · **Weight:** 2% · **Automation:** deterministic

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | dockerfile | yes | Dockerfile present |
| ✅ | compose | yes | docker-compose for dependencies |
| ✅ | env-config | yes | externalized config (env vars, 12-Factor III/IV) |
| ✅ | pinning | yes | pinned dependencies (lock file / global.json / Central Package Management) |
| ❌ | ci | no | CI pipeline present |
| ✅ | non-root | yes | container runs as non-root |
| ❔ | hadolint | indeterminate<br/>_tool 'hadolint' not installed — install: https://github.com/hadolint/hadolint_ | Dockerfile with no violations (hadolint) |
| ❔ | outdated | indeterminate<br/>_tool 'dotnet-outdated' not installed — install: dotnet tool install -g dotnet-outdated-tool_ | dependencies reasonably up to date |

> ⚠️ Missing tools: hadolint, dotnet-outdated

## 13. Documentation 🟠

**Score:** 4.2/5 · **Weight:** 1% · **Automation:** proxy

| Status | Metric | Observed | Target |
|--------|--------|----------|--------|
| ✅ | readme | yes | README present |
| ✅ | readme-sections | 3/3 | README with purpose+setup+run |
| ✅ | api-docs | yes | API documentation (OpenAPI/Swagger) |
| ❌ | doc-comments | no | doc comments / XML docs |
| ❔ | markdownlint | indeterminate<br/>_tool 'markdownlint' not installed — install: npm i -g markdownlint-cli_ | README passes markdownlint |
| ❔ | links | indeterminate<br/>_tool 'lychee' not installed — install: https://github.com/lycheeverse/lychee_ | no broken links (lychee) |

> ℹ️ PROXY: README section/link presence, OpenAPI completeness and doc-comment coverage are scored automatically.
> ⚠️ Missing tools: markdownlint, lychee

