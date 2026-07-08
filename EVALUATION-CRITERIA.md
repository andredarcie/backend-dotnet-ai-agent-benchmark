# Evaluation Criteria — Backend Project (Web API, Messaging, and Database)

This document defines the **evaluation categories** applicable to a backend project that exposes a **REST Web API**, integrates **messaging** (event/message broker), and persists data in a **database**.

The goal is to provide a consistent, comparable, and auditable criterion for **fully automated** benchmarking across different implementations — every score is produced by the tool, with no human (or LLM) in the loop.

> **This version has been reviewed against external sources** to seek consensus. The categories are anchored in recognized standards and references (ISO/IEC 25010, OWASP, IETF/RFCs, Microsoft/Google API guidelines, 12-Factor, microservices.io, Confluent/Kafka, Google SRE, OpenTelemetry, DORA, PCI DSS). See the [Critical review and source consensus](#critical-review-and-source-consensus) section and the full list in [Sources reviewed](#sources-reviewed).

---

## How to use this document

- Each category receives a **score from 0 to 5** (see scale below) and a suggested **weight**.
- The final score is the weighted average of the categories.
- Each category lists **what to look for** (criteria), **quality signals**, the **consensus basis** (sources backing the criterion), and the **automated evaluation** (🤖 — how to measure it objectively and reproducibly).

### Principle of automated evaluation

> Whenever possible, the score should derive from an **objective, reproducible measurement that runs in CI**, with no human judgment. Each category includes a **🤖 Automated evaluation** block with three elements: **Method** (how to measure), **Metric / threshold** (the number that becomes the score), and **Tools** (focused on the project stack — **.NET 10 / PostgreSQL / Kafka** — with generic equivalents). Where a category is partly subjective (e.g., architectural clarity), measurable *proxies* are used (coupling metrics, rule-violation counts) and scored automatically — never by human triage.
>
> **Constraint — 100% local execution:** all tools must run locally, with no dependency on SaaS/cloud services. Where a cloud variant exists, the local one is used: **self-hosted SonarQube** (Docker) instead of SonarCloud; local **CodeQL CLI**; **`act`** to run GitHub Actions on the machine; Postgres metrics via **pg_stat_statements/pgBadger** instead of SaaS. Common prerequisite: **Docker** (Testcontainers, ephemeral brokers and databases).
>
> **Scope of "local" (not *air-gapped*):** some steps need the **network** to download data — not a third-party service: CVE feeds (**Trivy** / **OWASP Dependency-Check** / `dotnet list --vulnerable`), `nuget restore`, and external link checking (**lychee**). To run 100% offline, mirror these feeds. Defaults chosen to avoid license nuance in local execution: **Semgrep OSS** (instead of CodeQL) and **Apicurio Registry** (Apache-2, instead of the Confluent Schema Registry).
>
> **Metric → score (0–5) mapping rule:** define *thresholds* per metric and convert. Generic example: target met with margin = 5; target met = 4; close to target = 3; below = 2; far below = 1; absent/not measured = 0. The suggested *thresholds* below are a starting point.

### Score scale

| Score | Meaning |
|------|-------------|
| 0 | Absent or non-functional |
| 1 | Present, but with serious flaws |
| 2 | Works on the happy path, fragile |
| 3 | Adequate, follows the expected basics |
| 4 | Solid, with good practices applied |
| 5 | Exemplary, production-ready |

### About the weights

> **There is no external consensus on weights.** ISO/IEC 25010 itself defines *what* to evaluate but deliberately does **not** prescribe weights — the relative importance depends on the context and the product's requirements. The weights below are a **proposed calibration** for the context of this benchmark (a credit card API, a critical domain → more weight on Correctness, Security, Messaging, and Persistence) and should be adjusted deliberately.

| # | Category | Related ISO 25010 attribute | Weight | Automation |
|---|-----------|--------------------------------|------|-----------|
| 1 | Functional Suitability / Correctness | Functional suitability | 12% | 🟡 |
| 2 | Architecture and Design | Maintainability | 10% | 🟠 |
| 3 | Code Quality | Maintainability | 8% | 🟢 |
| 4 | REST API Design | Compatibility / Interoperability | 11% | 🟡 |
| 5 | Persistence and Database | Reliability / Performance | 10% | 🟠 |
| 6 | Messaging | Reliability / Compatibility | 11% | 🟢 |
| 7 | Security | Security | 12% | 🟠 |
| 8 | Resilience and Error Handling | Reliability | 8% | 🟢 |
| 9 | Testing (*enabler*) | — (means, not an attribute) | 8% | 🟢 |
| 10 | Observability (*enabler*) | — (means, not an attribute) | 4% | 🟢 |
| 11 | Performance and Scalability | Performance efficiency | 3% | 🟡 |
| 12 | Portability, Configuration, and Deployment | Portability | 2% | 🟢 |
| 13 | Documentation | Maintainability / Usability | 1% | 🟠 |

**Measurement legend — every category is scored 100% by machine; the colour only marks how *directly* it is measured (no human is ever in the loop):**
- 🟢 **Deterministic** — scored 100% by machine from static analysis; the same source always produces the same score.
- 🟡 **Oracle** — scored 100% by machine on every run against an oracle/threshold defined once per benchmark (see [Oracle setup](#oracle-setup-defined-once-per-benchmark)).
- 🟠 **Proxy** — scored 100% by machine from an objective *proxy* metric (coupling, rule-violation counts, presence checks). Less direct than a deterministic count, but still fully automated.

---

## Critical review and source consensus

Coldly comparing the original list (12 categories) against the researched sources, here is what was confirmed, what was missing, and what was adjusted.

### Review findings

1. **"Functional Suitability / Correctness" was missing — the biggest gap.** ISO/IEC 25010 places *Functional suitability* (completeness, correctness, and appropriateness of functions) as the **first** quality attribute. The original list started straight at "Architecture" and never explicitly assessed whether the API **does what it should**. In a benchmark, this is the most basic criterion. → **New category added (no. 1).**

2. **"Testing" and "Observability" are not quality attributes — they are *means*.** ISO 25010 does not list them as product quality characteristics; they are *enablers* (ways to obtain and evidence quality). Kept as evaluable categories (they are observable in the code and relevant to the benchmark), but **marked as *enablers*** to make the distinction explicit.

3. **"Messaging" and "Persistence" are not standalone ISO attributes** — they are **technical subdomains** where Reliability, Performance, Security, and Compatibility manifest. Since this project **explicitly** uses a database + broker, keeping them as concrete categories is justifiable (they instantiate cross-cutting attributes in a specific technology). I have only documented the relationship.

4. **"Portability" was implicit only under DevOps.** ISO 25010 treats *Portability* (installability, adaptability, replaceability) as a first-class attribute, and the **12-Factor App** provides the operational consensus (config in env vars, pluggable backing services). The DevOps category was renamed to **"Portability, Configuration, and Deployment"** to cover this, and the **DORA metrics** were cited as a reference for delivery performance.

5. **The remaining categories were confirmed by consensus**, with specific anchor sources:
   - *REST API* → Richardson Maturity Model, Microsoft REST Guidelines, RFC 9457 (Problem Details), RFC 9110 (HTTP).
   - *Security* → OWASP API Security Top 10 (2023) and, for the domain, PCI DSS Requirement 3.
   - *Resilience* → the ISO *Reliability* attribute + retry/circuit breaker/idempotency patterns.
   - *Performance* → the *Performance efficiency* attribute + Google SRE's *Four Golden Signals*.
   - *Observability* → the three pillars (logs/metrics/traces) of OpenTelemetry/CNCF.

6. **Weights have no consensus backing** — the presentation was corrected to make this explicit (see above).

### ISO/IEC 25010 coverage map → categories

ISO/IEC 25010:2023 defines 9 product quality characteristics. Mapping to verify that nothing relevant was left out:

| ISO 25010 characteristic | Covered by |
|--------------------------|-------------|
| Functional suitability | Cat. 1 (Functional Suitability / Correctness) |
| Performance efficiency | Cat. 11 (Performance and Scalability) |
| Compatibility | Cat. 4 (REST API), Cat. 6 (Messaging) |
| Usability | Cat. 4 (API as "developer UX"), Cat. 13 (Documentation) |
| Reliability | Cat. 5 (Persistence), Cat. 6 (Messaging), Cat. 8 (Resilience) |
| Security | Cat. 7 (Security) |
| Maintainability | Cat. 2 (Architecture), Cat. 3 (Code Quality), Cat. 13 (Docs) |
| Portability | Cat. 12 (Portability, Config, and Deployment) |
| Safety (new in 2023) | Partial — Cat. 7/8 (relevant to critical domains; not core for CRUD) |

> *Usability* in ISO refers to the end user; in a "headless" API we reinterpret it as **developer experience** (contract clarity + documentation). Conclusion: **all 9 characteristics are covered**; "Testing" and "Observability" complement them as *enablers*.

---

## Oracle setup (defined once per benchmark)

The 🟡 categories only become automatic scores **after** defining, once, the "answer key" against which each implementation is measured. This does **not** repeat on every run — it is the benchmark's initial setup.

- **Cat. 1 — Correctness:** a black-box acceptance test suite with the **expected results** of the domain flows.
- **Cat. 4 — REST API:** the **expected status codes and contracts** per endpoint (in the reference OpenAPI).
- **Cat. 11 — Performance:** the **target SLO** (p95/p99 latency, minimum RPS, maximum error rate).
- **Cat. 2 — Architecture (partial):** the **layer rules** (which namespaces are domain / application / infra) for the *fitness functions*.
- **Cat. 7 — Security (partial):** the **BOLA test** scenario (user A × user B's resource) and the list of forbidden patterns (PAN/CVV).

The 🟠 categories (2, 5, 7, 13) are scored **100% automatically** from objective proxies once the oracle is configured — SAST/DAST tool output, 3NF/schema-shape heuristics, the overengineering metrics (class size, single-implementation-interface ratio) and documentation completeness (section/OpenAPI/doc-comment presence). No human verdict is applied; a proxy is simply less direct than a deterministic count.

---

## 1. Functional Suitability / Correctness 🟡

Evaluates whether the system **does what it should do**, completely and correctly. It is ISO/IEC 25010's attribute no. 1.

**What to look for**
- Completeness: all specified requirements/endpoints exist and respond.
- Correctness: the results are right (calculations, business rules, states).
- Appropriateness: the operations actually meet the goal (e.g., card creation, transaction, billing genuinely work end to end).
- Business rules and domain validations applied (limits, balances, valid statuses).
- Consistency between what the API promises (contract) and what it delivers.

**Quality signals**
- Complete flows work end to end (HTTP → rule → persistence → event).
- Domain edge cases handled (boundary values, invalid states).
- No "façade" functionality (an endpoint that exists but does not deliver the effect).

📚 **Consensus basis:** ISO/IEC 25010 — *Functional suitability* (functional completeness, correctness, appropriateness).

🤖 **Automated evaluation**
- **Method:** a **black-box** acceptance/integration test suite running all flows against the real API, with real dependencies brought up by **Testcontainers** (Postgres + Kafka); fuzzing/property-based derived from the **OpenAPI** contract; **mutation testing** to prove the tests actually catch defects (passing isn't enough — they must detect breakage).
- **Metric / threshold:** % of specified endpoints implemented and correct (target **100%**); acceptance test pass rate (**100%**); **mutation score ≥ 70%** (Stryker.NET).
- **Tools (local):** xUnit/NUnit + `WebApplicationFactory` + **Testcontainers**; **Schemathesis** (property-based via OpenAPI); **Postman/Newman** or `.http` files; **Stryker.NET** (mutation). Generic: REST-assured, schemathesis, PIT.

---

## 2. Architecture and Design 🟠

Evaluates the macro-level organization of the system and the separation of responsibilities. Underpins ISO 25010's *Maintainability*.

**What to look for**
- Clear separation of layers/contexts (presentation, application, domain, infrastructure).
- Direction of dependencies (business rules do not depend on infra details).
- Cohesion and low coupling between modules (modularity).
- Sound domain modeling (entities, aggregates, value objects where applicable).
- Architectural decisions documented and justified.
- **Simplicity / absence of *overengineering* (YAGNI):** complexity proportional to the problem — no layers, abstractions, generalizations, or *design patterns* applied "just in case", without a real point of variation to justify them.

**Quality signals**
- Swapping the database/broker/framework does not require rewriting business rules.
- Transaction and consistency boundaries well defined.
- No "god classes" and no business logic in controllers.
- The solution is the **simplest that meets** the requirements; abstractions exist because there are ≥2 real uses/variations, not out of speculation.

📚 **Consensus basis:** ISO/IEC 25010 — *Maintainability* (modularity, reusability, modifiability); Microsoft Azure Architecture Center (Microservices design).

🤖 **Automated evaluation**
- **Method:** *architecture fitness functions* — tests that **assert dependency rules** between layers (e.g., the domain does not reference infrastructure); detection of **dependency cycles**; coupling/instability metrics.
- **Metric / threshold:** architecture rule violations = **0**; dependency cycles = **0**; instability (I) and distance from the main sequence (D) per module within a healthy range; no "god classes" (LOC/responsibilities per class above a limit = flag). ***Overengineering* proxies:** interfaces/abstractions with a **single implementation** close to 0 (when there is no real point of variation), low depth of inheritance (DIT), **0 unused public abstractions/members**.
- **Tools (local):** **NetArchTest** or **ArchUnitNET** (rules as tests — including a custom "interface with 1 implementation" rule); **Roslyn analyzers** + **ReSharper CLI** (`jb inspectcode`) for unused abstractions/code; **NDepend** (local; CQLinq, coupling/cycles — commercial, optional); self-hosted **SonarQube** (Docker). Generic: ArchUnit (JVM), dependency-cruiser.

---

## 3. Code Quality 🟢

Evaluates readability and maintainability at the micro level (ISO 25010's *analysability* and *modifiability*).

**What to look for**
- Consistent, expressive naming.
- Short functions/methods, single responsibility.
- No significant duplication.
- Explicit null/error handling (no *swallowing* of exceptions).
- Language/framework conventions respected (idiomatic).
- No dead code, obsolete comments, or pending `TODO`s.
- **Absence of premature optimization / accidental complexity:** no micro-optimizations that sacrifice readability (manual caching, `unsafe`, exotic data structures, *pooling*) without evidence of a measured bottleneck.

**Quality signals**
- Linter/formatter configured and free of violations.
- Uniform conventions across the whole project.
- Self-explanatory code; comments reserved for the "why".
- Code solves the problem directly; any optimizations present are **justified by measurement**, not by intuition.

📚 **Consensus basis:** ISO/IEC 25010 — *Maintainability* (analysability, modifiability, testability).

🤖 **Automated evaluation**
- **Method:** static analysis + formatting checks in CI, all as pipeline *gates* (fails the build on violation).
- **Metric / threshold:** cyclomatic complexity **≤ 10** and cognitive **≤ 15** per method; **duplication ≤ 3%**; SonarQube *maintainability rating* = **A**; low *technical debt ratio*; **0 warnings** with `TreatWarningsAsErrors`; **0 dead code** (unused-member analyzers). **Premature optimization:** micro-optimizations (`unsafe`, manual cache, *pooling*) are only accepted with a **benchmark proving the gain** — otherwise they count as accidental complexity and lower the score.
- **Tools (local):** **self-hosted SonarQube** (Docker — smells, duplication, complexity, technical debt); **Roslyn analyzers** via `.editorconfig` (including dead code IDE0051/IDE0052); **ReSharper CLI** (`jb inspectcode`) for dead code/redundancies; `dotnet format --verify-no-changes`; `dotnet` code metrics (maintainability index). Generic: ESLint, RuboCop, golangci-lint.

---

## 4. REST API Design 🟡

Evaluates the exposed HTTP contract. Underpins *Compatibility/Interoperability* and the "developer UX".

**What to look for**
- Resource modeling and correct use of HTTP verbs (GET, POST, PUT, PATCH, DELETE).
- Coherent status codes (200, 201, 204, 400, 401, 403, 404, 409, 422, 500) — practical target: **Level 2 of the Richardson Maturity Model**.
- Consistent payload structure (naming, date formats).
- Pagination, filtering, and sorting on collections.
- Input validation and **standardized errors via RFC 9457 (Problem Details, `application/problem+json`)**, successor to RFC 7807.
- API versioning.
- Idempotency on sensitive operations (idempotency key).
- Contract documentation (OpenAPI/Swagger).

**Quality signals**
- Predictable, stable contract; structured, actionable errors.
- Separation between input/output DTOs and domain entities.
- HTTP semantics respected (idempotency of PUT/DELETE, safety of GET).

📚 **Consensus basis:** Richardson Maturity Model (Fowler); Microsoft REST API Guidelines; Azure Architecture Center (API design); IETF RFC 9457 (Problem Details); RFC 9110 (HTTP Semantics).

🤖 **Automated evaluation**
- **Method:** **OpenAPI contract lint** with a ruleset; **contract ↔ implementation conformance** testing (does the real response match the schema?); automatic **breaking-change** checks between versions; verification that errors return `application/problem+json`.
- **Metric / threshold:** **Spectral** violations of `error` severity = **0**; error responses in **RFC 9457** = **100%**; status code correctness (validated by tests); **Richardson level ≥ 2**; 0 unversioned breaking changes.
- **Tools (local):** **Spectral** (OpenAPI lint, custom ruleset); **Schemathesis** (contract↔runtime conformance); **openapi-diff**/oasdiff (breaking changes); OpenAPI generation via Swashbuckle/NSwag to validate the published contract.

---

## 5. Persistence and Database 🟠

Evaluates database usage and the data access layer (contributes to *Reliability* and *Performance*).

**What to look for**
- Schema modeling (normalization — typically **3NF** as the target —, correct types, constraints).
- **Referential integrity** via PK/FK at the database level (prevents orphaned records).
- Indexes consistent with query patterns (incl. FK columns and filter/join/sort columns).
- Versioned, reproducible migrations.
- Transaction management and an appropriate isolation level.
- Concurrency control (optimistic/pessimistic) where needed.
- No N+1 and no inefficient queries.
- Handling of sensitive data (see Security).

**Quality signals**
- The schema evolves via migrations, never manually.
- Predictable, indexed queries; integrity guaranteed by the database.
- Connection pooling and timeouts configured.

📚 **Consensus basis:** Relational design best practices (normalization 1NF→3NF/BCNF, referential integrity, workload-oriented indexing). See the database design sources.

🤖 **Automated evaluation**
- **Method:** apply **migrations on a clean database** (Testcontainers) and validate idempotency; analyze the **execution plan** (`EXPLAIN ANALYZE`) of hot-path queries; **detect N+1** via query logs during tests; query the **Postgres catalog** to verify FKs and indexes.
- **Metric / threshold:** every FK has a supporting index (a query joining `pg_constraint` × `pg_indexes`) = **100%**; **0** N+1 queries in critical flows; **0** *sequential scans* on hot paths under volume; migrations come up from scratch without error; schema in **3NF** (except for justified denormalization).
- **Tools (local):** `EXPLAIN (ANALYZE, BUFFERS)` + **pg_stat_statements**; **SchemaCrawler** (schema lint/diff); **sqlfluff** (SQL lint); **EF Core** / MiniProfiler logs to flag N+1. Generic (local): **pgBadger** (log analysis), **HypoPG** (hypothetical indexes).

---

## 6. Messaging 🟢

Evaluates asynchronous integration via a broker (Kafka, RabbitMQ, etc.). Contributes to *Reliability* and *Compatibility*.

**What to look for**
- Clear definition of topics/queues and their responsibilities.
- Versioned, documented message/event schema (event contract).
- Delivery semantics handled: the real-world default is **at-least-once → idempotent consumers** (check whether `event_id` was already processed before applying effects).
- Failure handling: retry, backoff, **dead-letter queue (DLQ)**.
- Ordering and partitioning (partition key) when relevant.
- Offset commit / ack **after** successful processing (never before).
- **Consistency between database and broker via the Transactional Outbox Pattern** (solves the *dual-write* problem).
- On "exactly-once": Kafka's EOS holds **within** Kafka (consume→process→produce with transactions + `isolation.level`); when integrating external systems, the recommendation is **idempotency**, not distributed EOS.

**Quality signals**
- Idempotent consumers — reprocessing does not duplicate effects.
- Messages are not lost on consumer failure.
- Production/consumption decoupled from the HTTP request cycle.
- DLQ monitored with a reprocessing strategy.

📚 **Consensus basis:** microservices.io — Transactional Outbox & Idempotent Consumer (Chris Richardson); Confluent/Apache Kafka — Delivery Semantics & Exactly-Once.

🤖 **Automated evaluation**
- **Method:** integration tests with **real Kafka (Testcontainers)**: (1) publish the **same event twice** and assert a **single effect** (idempotency); (2) **kill the consumer mid**-processing and verify lossless reprocessing (at-least-once); (3) validate **schema compatibility**; (4) inspect the **Outbox** table and **DLQ**.
- **Metric / threshold:** the idempotency test passes (duplicate → **1** effect); the message is reprocessed after a crash (loss = **0**); **BACKWARD/FULL** schema compatibility in the Schema Registry = ok; the Outbox is drained (pending rows → 0); DLQ configured and tested; *consumer lag* stable under load.
- **Tools (local):** **Testcontainers-Kafka**; **Schema Registry** compatibility check (Avro/Protobuf/JSON Schema); **Pact** (message contract testing); fault injection by killing the consumer process in the test.

---

## 7. Security 🟠

Evaluates the protection of the application, its data, and its integrations (ISO 25010's *Security*).

**What to look for**
- Authentication and authorization — special attention to **BOLA/Broken Object Level Authorization (OWASP API #1)** and Broken Authentication (#2).
- Secrets management (no *hardcoded* credentials; env vars / secret manager).
- Validation and sanitization of **all** inputs (injection); beware of *mass assignment* / *excessive data exposure* (OWASP API #3).
- **Unrestricted Resource Consumption (OWASP API #4)** → rate limiting / throttling.
- Security Misconfiguration (#8), SSRF (#7), Unsafe Consumption of APIs (#10).
- Sensitive data in transit (TLS) and at rest.
- Principle of least privilege (database, broker, services).
- Logs without leaking sensitive data (passwords, tokens, PII).

**Quality signals**
- No secrets in the repository or in logs; *deny by default*.
- **Domain compliance — PCI DSS Requirement 3 (card data):** PAN protected by strong encryption / truncation (at most first 6 + last 4) / tokenization / hashing; **sensitive authentication data (full track, CVV/CVC, PIN) never stored post-authorization**; cryptographic key management.

📚 **Consensus basis:** OWASP API Security Top 10 (2023); ISO/IEC 25010 — *Security* (confidentiality, integrity, authenticity, accountability, non-repudiation); PCI DSS v4.x Requirement 3.

🤖 **Automated evaluation**
- **Method:** a layered security pipeline — **SAST** (code), **DAST** (running API), **SCA** (dependencies), **secret scanning** (git history), and **PCI-specific checks** (scanning for PAN/CVV in logs and columns; **BOLA** authorization test: user A tries to access user B's resource and must receive 403/404).
- **Metric / threshold:** **High/Critical vulnerabilities = 0** (SAST+SCA); **secrets in history = 0**; BOLA/BFLA test passes (cross access denied); **0** occurrences of forbidden storage (CVV/track/PIN); high-risk DAST alerts triaged; TLS mandatory.
- **Tools (local):** **OWASP ZAP** (DAST via OpenAPI); **CodeQL**/**Semgrep** (SAST); `dotnet list package --vulnerable` + **Trivy**/**OWASP Dependency-Check** (SCA); **gitleaks**/**trufflehog** (secrets); custom regex/Semgrep for PAN patterns.

---

## 8. Resilience and Error Handling 🟢

Evaluates behavior under failure (*Reliability*: fault tolerance, recoverability).

**What to look for**
- Centralized, consistent exception handling.
- Timeouts, retries with backoff, and circuit breakers on external calls.
- Graceful degradation (one component's failure does not bring everything down).
- Idempotency on re-executable operations.
- Health checks (liveness/readiness).
- Graceful shutdown (drain requests/consumption before terminating).

**Quality signals**
- Transient failures absorbed without manual intervention.
- Unhandled errors do not leak stack traces to the client.
- The system recovers from database/broker outages.

📚 **Consensus basis:** ISO/IEC 25010 — *Reliability* (maturity, availability, fault tolerance, recoverability); resilience patterns (retry/backoff, circuit breaker, bulkhead).

🤖 **Automated evaluation**
- **Method:** controlled **fault injection** — introduce latency/outage in Postgres and Kafka (via **Toxiproxy**) and measure whether the application recovers; test **health checks** (liveness/readiness reflect dependencies); test **graceful shutdown** (SIGTERM drains requests); verify that errors **do not leak a stack trace** to the client.
- **Metric / threshold:** automatic recovery after a transient failure (no manual restart); `/health/ready` reflects the real state of dependencies; error responses **without a stack trace** (validated by DAST); presence of retry/timeout/circuit breaker policies at external I/O points.
- **Tools (local):** **Toxiproxy** (or Testcontainers pausing containers) for *fault injection*; **Polly** (detect usage and cover with tests); **ASP.NET HealthChecks**; **k6** with failure scenarios. Generic: Chaos Toolkit, Resilience4j.

---

## 9. Testing *(enabler)* 🟢

> *Not an ISO 25010 quality attribute, but the main means of evidencing it.* Evaluated because it is observable in the code.

**What to look for**
- Unit tests for business rules (base of the pyramid).
- Integration tests for the API, database, and messaging.
- **Contract tests** (consumer-driven, e.g., Pact) for the API and/or events.
- Error and edge scenarios, not just the happy path.
- Independence and determinism (no *flakiness*).
- Use of *test containers* / ephemeral environments for dependencies.
- Coverage of critical paths (not just raw %).

**Quality signals**
- The suite runs in CI and is reliable.
- Tests serve as living documentation of behavior.
- Easy to run locally with a single command.

📚 **Consensus basis:** Practical Test Pyramid (Martin Fowler); Consumer-Driven Contract Testing (Pact; Microsoft Engineering Playbook).

🤖 **Automated evaluation**
- **Method:** measure test **coverage** and **strength**; **classify the pyramid** (count by type); **detect flakiness** by running the suite N times and comparing results.
- **Metric / threshold:** **line/branch coverage ≥ 80%** on critical paths; **mutation score ≥ 70%** (coverage alone is misleading — mutation measures whether the tests *detect* bugs); a healthy pyramid ratio (a wide base of unit tests); **0 flaky tests** over ~10 runs; the suite runs in CI.
- **Tools (local):** **Coverlet** + **ReportGenerator** (coverage, cobertura/lcov formats); **Stryker.NET** (mutation); CI reruns for flakiness. Generic: JaCoCo, PIT, Istanbul.

---

## 10. Observability *(enabler)* 🟢

> *Also a means, not an ISO attribute.* Essential for operating the system.

**What to look for**
- **Three pillars: logs, metrics, and traces** (the OpenTelemetry/CNCF model), with correlation (trace/correlation id).
- Metrics aligned with **Google SRE's Four Golden Signals: latency, traffic, errors, and saturation** (+ *consumer lag* in messaging).
- Distributed tracing across the API, database, and broker.
- Structured logging with appropriate levels.
- Health/diagnostic endpoints.

**Quality signals**
- Diagnose an incident without adding code.
- End-to-end correlation of an operation (HTTP → event → consumption).

📚 **Consensus basis:** Google SRE — *Four Golden Signals*; OpenTelemetry / CNCF — three pillars (logs, metrics, traces).

🤖 **Automated evaluation**
- **Method:** verify the presence and **functioning** of the instrumentation (not just the dependency) — an **end-to-end trace/correlation id propagation** test (HTTP → event production → consumption); validate the structured log format; *scrape* the metrics endpoint; check that the 4 signals are exposed.
- **Metric / threshold:** **OpenTelemetry** SDK present covering **traces + metrics + logs**; correlation id propagated end to end (verified in an integration test); **4 Golden Signals** exposed (latency/traffic/errors/saturation) **+ consumer lag**; `/health` and `/metrics` respond; logs in **structured JSON**.
- **Tools (local):** **OpenTelemetry .NET** (+ an in-memory Collector in the test); **Prometheus** scrape test; a log-format validator; trace-context assertion in an integration test.

---

## 11. Performance and Scalability 🟡

Evaluates efficiency and the ability to scale (*Performance efficiency*: time behavior, resource utilization, capacity).

**What to look for**
- Efficient use of resources (CPU, memory, connections).
- Asynchronous/non-blocking I/O where appropriate.
- Caching strategies where they make sense.
- API *statelessness* (horizontal scalability).
- Absence of obvious bottlenecks (locks, heavy queries on the *hot path*).
- **Measurement-guided optimization, not speculative:** micro-optimizations that add complexity without a benchmark-proven gain are *premature optimization* and should be penalized (see Cat. 2 and 3). Optimize what the profiler/load test flagged as a real bottleneck.

**Quality signals**
- Stateless API, horizontally scalable.
- Parallelizable message consumption.
- No connection/memory leaks under load.

📚 **Consensus basis:** ISO/IEC 25010 — *Performance efficiency*; 12-Factor (*stateless* processes, factor VI).

🤖 **Automated evaluation**
- **Method:** a **load test** with a realistic profile against the API (with real Postgres + Kafka); **micro-benchmark** of the hot paths; **soak test** (sustained load) to detect leaks; verify **statelessness** (no session state in memory).
- **Metric / threshold:** **p95/p99** latency under target load within SLO; **maximum throughput (RPS)** before violating the SLO; **error rate under load ≈ 0%**; memory/CPU stable in the soak (no *leak*); results versioned to compare implementations.
- **Tools (local):** **k6** or **NBomber** (load); **BenchmarkDotNet** (hot paths); **dotnet-counters**/**dotnet-trace** (runtime resources). Generic: Gatling, JMeter, wrk.

---

## 12. Portability, Configuration, and Deployment 🟢

Evaluates reproducibility and operation (*Portability*: installability, adaptability, replaceability), guided by 12-Factor.

**What to look for**
- **Configuration externalized in environment variables** (12-Factor III) — no secrets/config in the code.
- **Pluggable backing services** (database/broker treated as attached resources, swappable by config alone — 12-Factor IV).
- Reproducible build and pinned dependencies (12-Factor I/II).
- Containerization (Dockerfile, docker-compose for dependencies).
- CI pipeline (build + tests + lint).
- Clear deployment strategy.

**Quality signals**
- `clone → up → run` works on any machine.
- CI blocks the merge when the build/tests fail.
- No environment configuration embedded in the code.

📚 **Consensus basis:** The Twelve-Factor App (Config, Backing services, Build/Release/Run, Stateless processes); ISO/IEC 25010 — *Portability*. For **delivery** performance, a complementary reference: the **DORA metrics** (deployment frequency, lead time, change failure rate, time to restore) — note that these are *process* metrics, not assessable from a static code snapshot.

🤖 **Automated evaluation**
- **Method:** an automated **clean clone → `docker compose up` → smoke test** (does it come up from scratch on a fresh machine?); **scanning for hardcoded config/secrets**; verification of dependency **pinning**; checking the **CI pipeline** status.
- **Metric / threshold:** `docker build` and `compose up` bring up **all** dependencies; the app starts reading **only env vars** (12-Factor III/IV) — **0** sensitive config in the code; dependencies **pinned** (lock files); **green CI** (build + test + lint); Dockerfile free of lint violations.
- **Tools (local):** **Docker** + **docker-compose**; **hadolint** (Dockerfile lint); a hardcoded config/env scanner (**gitleaks**/**Semgrep**); **dotnet-outdated**; **`act`** to run the GitHub Actions workflow locally (or run the pipeline steps directly in the shell).

---

## 13. Documentation 🟠

Evaluates support for understanding and operation (supports *Maintainability* and the developer's "Usability").

**What to look for**
- README with purpose, stack, and run instructions.
- API documentation (accessible OpenAPI/Swagger).
- Local setup instructions (dependencies, environment variables).
- ADRs (architectural decisions) when relevant.

**Quality signals**
- A new dev brings the project up following only the README.
- API and event contracts documented and up to date.

📚 **Consensus basis:** ISO/IEC 25010 — *Maintainability/Usability*; contract convention via OpenAPI.

🤖 **Automated evaluation**
- **Method:** **validate the OpenAPI spec**; check the README's **required sections**; measure **doc-comment coverage** of public contracts; **check for broken links**; test the "**clone → run** following only the README" (the same automation as Cat. 12).
- **Metric / threshold:** **valid** OpenAPI describing **100%** of the endpoints; README contains purpose + stack + setup + run; **0** broken links; the README setup reproduces the startup with no implicit steps.
- **Tools (local):** **Spectral**/`swagger-cli validate` (OpenAPI); **markdownlint**; **lychee** (link checker); **DocFX** (doc coverage). Generic: redocly lint, vale (prose).

---

## Scoring sheet (summary)

| # | Category | Weight | Score (0–5) | Weighted |
|---|-----------|------|------------|-----------|
| 1 | Functional Suitability / Correctness | 12% | | |
| 2 | Architecture and Design | 10% | | |
| 3 | Code Quality | 8% | | |
| 4 | REST API Design | 11% | | |
| 5 | Persistence and Database | 10% | | |
| 6 | Messaging | 11% | | |
| 7 | Security | 12% | | |
| 8 | Resilience and Error Handling | 8% | | |
| 9 | Testing (*enabler*) | 8% | | |
| 10 | Observability (*enabler*) | 4% | | |
| 11 | Performance and Scalability | 3% | | |
| 12 | Portability, Configuration, and Deployment | 2% | | |
| 13 | Documentation | 1% | | |
| | **Total** | **100%** | | |

> **Final score** = Σ (score × weight). Use the columns to record each implementation and allow direct comparison between projects.

---

## Sources reviewed

All sources consulted to review and anchor the categories above.

### Software quality model (umbrella framework)
- ISO — *ISO/IEC 25010:2023, Systems and software Quality Requirements and Evaluation (SQuaRE) — Product quality model*: https://www.iso.org/obp/ui/en/#!iso:std:78176:en
- ISO 25000 Portal — *ISO/IEC 25010*: https://iso25000.com/index.php/en/iso-25000-standards/iso-25010
- arc42 Quality Model — *ISO/IEC 25010*: https://quality.arc42.org/standards/iso-25010

### Security
- OWASP — *API Security Top 10 (2023)*: https://owasp.org/API-Security/editions/2023/en/0x11-t10/
- OWASP API Security Project (header 2023): https://owasp.org/API-Security/editions/2023/en/0x00-header/
- PCI DSS — *Requirement 3: Protect Stored Cardholder Data* (ISMS.online): https://www.isms.online/pci-dss/requirement-3/
- PCI DSS Guide — *Requirement 3 Explained*: https://pcidssguide.com/pci-dss-requirement-3/

### REST API design and HTTP contracts
- Microsoft Learn — *Web API design best practices (Azure Architecture Center)*: https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design
- Microsoft Learn — *Web API implementation*: https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-implementation
- Microsoft — *REST API Guidelines* (GitHub): https://github.com/microsoft/api-guidelines
- Martin Fowler — *Richardson Maturity Model*: https://martinfowler.com/articles/richardsonMaturityModel.html
- IETF — *RFC 9457: Problem Details for HTTP APIs* (obsoletes RFC 7807): https://www.rfc-editor.org/rfc/rfc9457.html

### Database
- Devart — *Database Design Best Practices*: https://www.devart.com/blog/database-design-best-practices.html
- Exasol — *Database Design Principles & Best Practices (Relational Guide)*: https://www.exasol.com/hub/database/design-principles/
- Netalith — *Database Normalization Techniques (1NF to BCNF)*: https://netalith.com/blogs/databases/database-normalization-techniques-step-by-step-guide-2026
- Acceldata — *Understanding Referential Integrity*: https://www.acceldata.io/blog/why-referential-integrity-matters-for-modern-data-systems

### Messaging / events
- microservices.io — *Pattern: Transactional outbox* (Chris Richardson): https://microservices.io/patterns/data/transactional-outbox.html
- Confluent — *Message Delivery Guarantees for Apache Kafka*: https://docs.confluent.io/kafka/design/delivery-semantics.html
- Confluent — *Exactly-once Semantics in Apache Kafka*: https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
- Confluent Developer — *Idempotent Reader*: https://developer.confluent.io/patterns/event-processing/idempotent-reader/

### Resilience and configuration
- The Twelve-Factor App — *Config*: https://12factor.net/config
- The Twelve-Factor App (full methodology): https://12factor.net

### Observability
- Google SRE Book — *Monitoring Distributed Systems (Four Golden Signals)*: https://sre.google/sre-book/monitoring-distributed-systems/
- OpenTelemetry — *Observability primer*: https://opentelemetry.io/docs/concepts/observability-primer/
- IBM — *Three Pillars of Observability: Logs, Metrics and Traces*: https://www.ibm.com/think/insights/observability-pillars

### Testing
- Martin Fowler — *The Practical Test Pyramid*: https://martinfowler.com/articles/practical-test-pyramid.html
- Microsoft — *Consumer-Driven Contract Testing (Engineering Playbook)*: https://microsoft.github.io/code-with-engineering-playbook/automated-testing/cdc-testing/

### DevOps / delivery performance
- DORA — *Software delivery performance metrics (Four Keys)*: https://dora.dev/guides/dora-metrics-four-keys/
- Google Cloud / DORA — *DevOps Research and Assessment*: https://dora.dev

> *Sources accessed: June 2026. The standards pages (ISO, IETF, OWASP, PCI SSC) are the normative references; the others are supporting/industry-consensus materials.*
