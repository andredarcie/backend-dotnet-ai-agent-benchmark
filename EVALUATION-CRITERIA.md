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
> **Constraint — 100% local, deterministic execution.** Every check runs locally, with no SaaS/cloud dependency, and the whole grading run is a single `docker compose up`. Two properties are non-negotiable, and they are what shape the tool list:
>
> 1. **Reproducible** — the same source must always produce the same score. A check whose verdict depends on a *remote* rule set (Semgrep `--config auto`), a CVE feed that changes daily (Trivy), or a reachable third-party URL (a link checker) is not a benchmark measurement: it grades the day, not the submission.
> 2. **Fast** — grading a submission is on the critical path of every run, so a check must earn its wall-clock. A DAST scan that adds 3–5 minutes to emit the same near-identical alert list for every submission does not.
>
> **Retired checks (and why).** The rubric previously cited OWASP ZAP (DAST), Semgrep (SAST), Trivy (SCA), Schemathesis (fuzzing), Stryker.NET (mutation), sqlfluff, Spectral, swagger-cli, markdownlint, lychee, dotnet-outdated, Toxiproxy, SonarQube, ReSharper CLI and k6. They are **no longer part of the score.** Each was slow, network-bound, non-reproducible, or redundant with a signal already measured directly — and together they were the bulk of the grading time. What remains is what actually discriminates between submissions:
>
> | Signal | How it is measured |
> |--------|--------------------|
> | Does the code compile, format and pass its own tests? | `dotnet build` / `dotnet format` / `dotnet test` + **Coverlet** |
> | Does the running system honor the contract? | the **live contract oracle** (drives the real API end to end) |
> | Did a real event reach the broker? | the harness **kafka-check** consumer (kcat) on the live topic |
> | What is actually in the source? | **Roslyn AST** (never regex) |
> | Secrets, vulnerable packages, Dockerfile | **gitleaks**, `dotnet list package --vulnerable`, **hadolint** |
>
> **One honest caveat.** This is *local*, not *air-gapped*: `dotnet restore` and the vulnerability audit behind `dotnet list package --vulnerable` still reach nuget.org, and that vulnerability data moves as CVEs are disclosed. So the **SCA metric is the one check that is not a pure function of the source** — a newly-disclosed CVE in an unchanged, pinned package can flip it. It is kept because dependency hygiene is a real production concern and it costs seconds, not minutes (unlike the tools it replaced); and when no NuGet source is reachable it is reported **Indeterminate**, never a silent Pass. Every other check — Roslyn, gitleaks, hadolint, the live oracle — is offline and deterministic.
>
> *The run is still the measurement* — a submission that never boots is capped, exactly as before (see the executability gate).
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

### Scored categories (weights sum to 100%)

| # | Category | Related ISO 25010 attribute | Weight | Automation |
|---|-----------|--------------------------------|------|-----------|
| 1 | Functional Correctness & Tests | Functional suitability | 20% | 🟡 |
| 2 | Architecture and Design | Maintainability | 12% | 🟠 |
| 3 | Code Quality | Maintainability | 10% | 🟢 |
| 4 | REST API Design | Compatibility / Interoperability | 14% | 🟡 |
| 5 | Persistence and Database | Reliability / Performance | 13% | 🟠 |
| 6 | Messaging | Reliability / Compatibility | 13% | 🟢 |
| 7 | Security | Security | 14% | 🟠 |
| 8 | Resilience and Error Handling | Reliability | 4% | 🟢 |

### Informational categories (measured, reported, **not scored**)

| # | Category | Why it does not carry weight |
|---|-----------|------------------------------|
| 9 | Observability | Its decisive signal is `/health`, and that is already the **executability gate** — a service that never answers it is capped at 1.5/5 no matter what. What remains (JSON logs, correlation id) is worth *showing*, not ranking. |
| 10 | Portability & Deploy | Whether the project deploys is not a checklist item here: the harness **boots the submission's own compose**. The gate already decides it; a 2% "a Dockerfile exists" score was double-counting. |
| 11 | Documentation | At 1%, the gap between a perfect README and none at all moved the final score by **0.05** — less than the run-to-run noise of the same model. That the OpenAPI document really describes the endpoints is asserted **live** in cat. 4, where it counts. |

> **Why cut 13 categories down to 8.** Three of the old categories (Documentation 1% + Portability 2% + Performance 3%) were **6% of the score combined** — arithmetically incapable of separating two submissions, but presented as peers of Security and Correctness. Meanwhile the same signal was being counted two and three times over: health statically in Resilience *and* statically in Observability *and* live *and* as the gate; a test project's existence in Correctness *and* in Testing; OpenAPI in REST Design *and* in Documentation; pagination statically in Performance *and* live in REST Design. The rubric now measures each thing **once, where it is strongest** — and reports the rest without pretending it decides anything.
>
> **Two categories were dissolved, not deleted.** *Testing* is folded into cat. 1: a suite the model writes to grade itself is a self-graded signal, so it belongs *next to* the independent oracle and outweighed by it — not standing alone at 8%. *Performance* is folded into cat. 3: once the load test was dropped it held nothing but two Roslyn facts (async I/O, sync-over-async — real code defects, now scored as such), one metric that could never fail (`stateless`) and one already asserted live (`pagination`).

**Measurement legend — every scored category is 100% machine-graded; the colour only marks how *directly* it is measured (no human is ever in the loop):**
- 🟢 **Deterministic** — scored 100% by machine from static analysis; the same source always produces the same score.
- 🟡 **Oracle** — scored 100% by machine on every run against an oracle/threshold defined once per benchmark (see [Oracle setup](#oracle-setup-defined-once-per-benchmark)).
- 🟠 **Proxy** — scored 100% by machine from an objective *proxy* metric (coupling, rule-violation counts, presence checks). Less direct than a deterministic count, but still fully automated.

---

## Critical review and source consensus

Coldly comparing the original list (12 categories) against the researched sources, here is what was confirmed, what was missing, and what was adjusted.

### Review findings

1. **"Functional Suitability / Correctness" was missing — the biggest gap.** ISO/IEC 25010 places *Functional suitability* (completeness, correctness, and appropriateness of functions) as the **first** quality attribute. The original list started straight at "Architecture" and never explicitly assessed whether the API **does what it should**. In a benchmark, this is the most basic criterion. → **New category added (no. 1).**

2. **"Testing" and "Observability" are not quality attributes — they are *means*.** ISO 25010 does not list them as product quality characteristics; they are *enablers* (ways to obtain and evidence quality). Both are still measured — but neither stands as a peer category any more: **Testing** is folded into cat. 1, beside (and outweighed by) the independent oracle, because a suite the model writes to grade itself is not an independent signal; **Observability** is reported as **informational** (weight 0), because its decisive signal (`/health`) is the executability gate.

3. **"Messaging" and "Persistence" are not standalone ISO attributes** — they are **technical subdomains** where Reliability, Performance, Security, and Compatibility manifest. Since this project **explicitly** uses a database + broker, keeping them as concrete categories is justifiable (they instantiate cross-cutting attributes in a specific technology). I have only documented the relationship.

4. **"Portability" was implicit only under DevOps.** ISO 25010 treats *Portability* (installability, adaptability, replaceability) as a first-class attribute, and the **12-Factor App** provides the operational consensus (config in env vars, pluggable backing services). The DevOps category was renamed to **"Portability, Configuration, and Deployment"** to cover this, and the **DORA metrics** were cited as a reference for delivery performance.

5. **The remaining categories were confirmed by consensus**, with specific anchor sources:
   - *REST API* → Richardson Maturity Model, Microsoft REST Guidelines, RFC 9457 (Problem Details), RFC 9110 (HTTP).
   - *Security* → OWASP API Security Top 10 (2023) and, for the domain, PCI DSS Requirement 3.
   - *Resilience* → the ISO *Reliability* attribute + retry/circuit breaker/idempotency patterns.
   - *Performance* → the *Performance efficiency* attribute. Its two real signals (async, non-blocking I/O; bounded/paginated responses) are now scored where they are strongest — as a **code defect** in cat. 3 and as a **live assertion** in cat. 4 — rather than as a 3%-weight category of their own.
   - *Observability* → the CNCF pillars of telemetry (logs / metrics / traces) as the **concept**; the benchmark reports structured logs and a correlation id, does **not** require the OpenTelemetry SDK or a `/metrics` endpoint, and does not rank on any of it.

6. **Weights have no consensus backing** — the presentation was corrected to make this explicit (see above).

7. **A category too light to decide anything should not pretend to be one.** Documentation at 1%, Portability at 2% and Performance at 3% were **6% of the score combined** — a perfect-vs-absent swing in Documentation moved the final by **0.05**, well inside the noise between two runs of the same model. They are now **informational**: still measured, still printed, never ranked. And the signals worth keeping were moved to where they are actually *proved* rather than *declared*.

### ISO/IEC 25010 coverage map → categories

ISO/IEC 25010:2023 defines 9 product quality characteristics. Mapping to verify that nothing relevant was left out:

| ISO 25010 characteristic | Covered by |
|--------------------------|-------------|
| Functional suitability | Cat. 1 (Functional Suitability / Correctness) |
| Performance efficiency | Cat. 3 (async / non-blocking I/O, scored as a code defect) and Cat. 4 (pagination, asserted live) |
| Compatibility | Cat. 4 (REST API), Cat. 6 (Messaging) |
| Usability | Cat. 4 (API as "developer UX" — the served OpenAPI, asserted live), Cat. 11 (Documentation, *informational*) |
| Reliability | Cat. 5 (Persistence), Cat. 6 (Messaging), Cat. 8 (Resilience) |
| Security | Cat. 7 (Security) |
| Maintainability | Cat. 2 (Architecture), Cat. 3 (Code Quality), Cat. 11 (Docs, *informational*) |
| Portability | The **executability gate** (the harness boots the submission's own compose) + Cat. 10 (*informational*) |
| Safety (new in 2023) | Partial — Cat. 7/8 (relevant to critical domains; not core for CRUD) |

> *Usability* in ISO refers to the end user; in a "headless" API we reinterpret it as **developer experience** (contract clarity + documentation). Conclusion: **all 9 characteristics are covered**; "Testing" and "Observability" complement them as *enablers*.

---

## Oracle setup (defined once per benchmark)

The 🟡 categories only become automatic scores **after** defining, once, the "answer key" against which each implementation is measured. This does **not** repeat on every run — it is the benchmark's initial setup.

- **Cat. 1 — Correctness:** the **expected results** of the domain flows — encoded once, as executable code, in the evaluator's `ContractOracle` (create card → create transaction → read → list → 404 → the validation rules).
- **Cat. 4 — REST API:** the **expected status codes and contracts** per endpoint (same oracle: `201` + `Location`, `problem+json` on a validation error, camelCase, an honored page size).
- **Cat. 2 — Architecture (partial):** the **layer rules** (which namespaces are domain / application / infra) for the *fitness functions*.
- **Cat. 7 — Security (partial):** the list of **forbidden patterns** — a Luhn-valid PAN in production code/config, and CVV/track/PIN fields. *(There is no BOLA scenario: auth is optional in this task and there is no user/ownership model to violate.)*

The 🟠 categories (2, 5, 7, 13) are scored **100% automatically** from objective proxies once the oracle is configured — the Roslyn AST facts (layering, class size, the single-implementation-interface ratio, schema shape, the PCI patterns), the gitleaks/NuGet-vulnerability output and documentation completeness (section/OpenAPI/doc-comment presence). No human verdict is applied; a proxy is simply less direct than a deterministic count.

---

## 1. Functional Correctness & Tests 🟡 — 20%

Evaluates whether the system **does what it should do**, completely and correctly. It is ISO/IEC 25010's attribute no. 1, and it is the heaviest category here.

> **Testing lives here now** (it used to be a standalone category no. 9). A suite the model writes is a **self-graded** signal — it can always author three trivial tests that pass — so it belongs *beside* the independent oracle and **outweighed by it**, not standing alone at 8%. The oracle carries roughly three quarters of this category's weight; the submission's own suite (does it exist, is it genuinely unit-only, what does it cover, does it pass) carries the rest.

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
- **Method:** the **live contract oracle** — the evaluator itself drives the documented flows black-box against the **running** system (create card → create transaction → read → list → 404s → the business rules: FK exists, `amount > 0`, `merchant` non-empty) and asserts the real request→response contract. This is the *independent* signal: it does not trust the submission's own tests, nor its self-declared OpenAPI. The submission's suite is then run **once** (`dotnet test --collect:"XPlat Code Coverage"`) — that single run yields both its pass rate and its coverage.
- **Metric / threshold:** contract-oracle checks passing (target **100%**) — *the bulk of the weight*; real unit tests present (`[Fact]`/`[Theory]`/`[Test]`); **unit-only** — a `Testcontainers` reference is a **Fail** (forbidden: it needs a Docker daemon and boots a Postgres/Kafka per run), a `WebApplicationFactory` reference is **partial credit** (in-process, but an acceptance test the task never asked for); **line coverage ≥ 60%** (full credit; half credit at ≥ 35%) — a **relaxed bar** on purpose, so a lean suite scores well and there's no incentive to pad; the suite's own pass rate (**100%**), at low weight because it is self-graded.
- **Coverage is merged, not sampled:** every `coverage.cobertura.xml` produced is unioned (one report per test project, so reading a single file would understate — or zero — a well-tested project), and pre-existing reports are deleted first, so a committed cobertura file cannot inflate the number.
- **Tools (local):** the evaluator's `ContractOracle` (real HTTP against the live API) + `dotnet test` + **Coverlet** + **Roslyn**. *The oracle **is** the acceptance suite — which is why the task asks the model for unit tests only: an independent oracle beats a suite the submission writes to grade itself, and a suite that boots its own broker is the single slowest thing in a grading run. Fuzzing (Schemathesis), mutation testing (Stryker.NET) and the flakiness reruns are retired: none justified its cost.*
- **Retired metrics:** `test-project` / `test-framework` / `coverage-tool` — three presence checks for one fact ("a test csproj exists"), one of them counted in *this* category and another in the old cat. 9. "A package is referenced" is not an engineering signal; `unit-tests` (does it declare real test cases?) replaces all three.

---

## 2. Architecture and Design 🟠 — 12%

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
- **Method:** *architecture fitness functions* — the Roslyn AST **asserts the dependency rules** between layers (the domain must not reference infrastructure), resolves the layer of each file, measures type size, and detects **machinery the brief explicitly ruled out**.
- **Metric / threshold:** architecture rule violations = **0** (`dependency-direction`); layers present (`layering`, `application-layer`); no "god classes" (largest type ≤ 600 lines); **`no-gold-plating` = 0 items** — see below.
- **YAGNI, made enforceable (`no-gold-plating`).** The task says overengineering is a defect; this is the metric that makes that true. It counts machinery the brief **explicitly told the model not to build**: `PUT`/`PATCH`/`DELETE` endpoints, a Kafka consumer, a transactional outbox, the OpenTelemetry SDK, API versioning, Testcontainers. **0 → Pass · 1–2 → half credit · ≥3 → Fail.** It replaces the old `overengineering-proxy`, which counted **single-implementation interfaces** — but those are the standard Dependency-Inversion seam (`IRepository`, `IProducer`) that *this very category rewards* under layering, so it could never fail without contradicting itself: in practice it was a free half-point for everyone. Ambition that ignores the spec is not engineering; it is not reading the brief.
- **Tools (local):** the **Roslyn AST** (`Microsoft.CodeAnalysis.CSharp`), in-process — it plays the NetArchTest/ArchUnit role here: it follows the `using` graph to catch a domain→infrastructure reference, measures type size, and detects the out-of-scope constructs above. No external analyzer is invoked.

---

## 3. Code Quality 🟢 — 10%

Evaluates readability and maintainability at the micro level (ISO 25010's *analysability* and *modifiability*).

> **Async/blocking I/O lives here now** (it used to be the separate "Performance and Scalability" category). Once the load test was dropped, that category held nothing but two Roslyn facts — async I/O and sync-over-async — plus one metric that could never fail (`stateless`) and one already asserted live in cat. 4 (`pagination`). A `.Result`/`.Wait()` on a request path in ASP.NET Core is a thread-pool starvation **bug**, so it is scored as a code defect (a **Fail**, not the half-credit it used to earn), not as a 3%-weight category of its own.

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
- **Metric / threshold:** **0 empty catches** (a swallowed exception, even with a comment); **0** pending `TODO`/`FIXME`/`HACK`; analyzers enabled (`.editorconfig` / `EnableNETAnalyzers` / `TreatWarningsAsErrors`); **0 build warnings** in Release; `dotnet format --verify-no-changes` clean; **async I/O present** and **0 sync-over-async** blocking calls (`.Result` / `.Wait()` / `.GetAwaiter().GetResult()`). **Premature optimization:** micro-optimizations (`unsafe`, manual cache, *pooling*) are only accepted with a **benchmark proving the gain** — otherwise they are flagged as accidental complexity.
- **Tools (local):** `dotnet format --verify-no-changes`; the analyzer warning count from the single Release build (the same build that gates executability — no second build is run); **Roslyn** for empty catches, TODOs, `unsafe`/`stackalloc` and the blocking-call detection. *SonarQube and ReSharper CLI are retired (heavy, and largely redundant with the analyzers already enforced by the build).*

---

## 4. REST API Design 🟡 — 14%

Evaluates the exposed HTTP contract. Underpins *Compatibility/Interoperability* and the "developer UX".

**What to look for**
- Resource modeling and correct use of the HTTP verbs **in scope** (`GET`, `POST` — the task's surface is read + create only; `PUT`/`DELETE` are deliberately out of scope and adding them is **penalized** as gold-plating in cat. 2, not credited).
- Coherent status codes (200, 201, 400, 404) — practical target: **Level 2 of the Richardson Maturity Model**. *Asserted live, not inferred from the source.*
- Consistent payload structure (camelCase, date formats).
- Pagination on collections — *asserted live: a requested page size is actually honored.*
- Input validation and **standardized errors via RFC 9457 (Problem Details, `application/problem+json`)**, successor to RFC 7807.
- Contract documentation (OpenAPI/Swagger) that **actually documents the endpoints**.

> **Not scored: API versioning** (`Asp.Versioning`, `/v1/` routing). There is one version of one API in scope; versioning it is ceremony, and the task now lists it as out of scope. **Not scored: idempotency keys** — nothing here re-executes a create.

**Quality signals**
- Predictable, stable contract; structured, actionable errors.
- Separation between input/output DTOs and domain entities.
- HTTP semantics respected (safety of `GET`, `201` + `Location` on create).

📚 **Consensus basis:** Richardson Maturity Model (Fowler); Microsoft REST API Guidelines; Azure Architecture Center (API design); IETF RFC 9457 (Problem Details); RFC 9110 (HTTP Semantics).

🤖 **Automated evaluation**
- **Method:** the **live contract oracle** observes the real response shape (the `Location` header on 201, the `application/problem+json` media type on a validation error, camelCase keys, whether a page size is actually honored); the **OpenAPI probe** fetches the served document and counts the operations it declares; Roslyn detects the verbs, `ProblemDetails` and DTOs in the source.
- **Metric / threshold:** the served OpenAPI declares **> 0 operations**; error responses in **RFC 9457** (`application/problem+json`); `201` carries a `Location`; JSON is camelCase; pagination honors a page size; **Richardson level ≥ 2**.
- **`openapi-populated` has three outcomes, and all three are scored:** served *and* declaring operations → **Pass**; served but **empty** (`"paths": {}`, e.g. `AddOpenApi()` never discovering the controllers) → **Fail**, because an empty contract is a real defect that presence-detection silently passes; **not served at all** → **Fail**, not "indeterminate" — the task requires an OpenAPI document, so its absence is a defect, not a measurement we failed to take.
- **Tools (local):** the evaluator's `ContractOracle` + `OpenApiProbe` (live HTTP) and **Roslyn**. *Spectral, swagger-cli and oasdiff are retired: linting a spec file says less than asserting the API's real answers, and most submissions generate the spec at runtime (there is no file to lint).*
- **Retired metrics:** the static `status-codes` check ("the source mentions `BadRequest` somewhere") — a strictly weaker proxy for the real 201/400/404 the oracle already asserts; the static `openapi` presence check — superseded by the live `openapi-populated` above, and it was scored a *second* time as `api-docs` under Documentation; `versioning` — see above.

---

## 5. Persistence and Database 🟠 — 13%

Evaluates database usage and the data access layer (contributes to *Reliability* and *Performance*).

**What to look for**
- Schema modeling (normalization — typically **3NF** as the target —, correct types, constraints).
- **Referential integrity** via PK/FK at the database level (prevents orphaned records).
- Indexes consistent with query patterns (incl. FK columns and filter/join/sort columns).
- Versioned, reproducible migrations.
- Transaction management and an appropriate isolation level.
- No N+1 and no inefficient queries.
- Handling of sensitive data (see Security).

> **Not scored: optimistic concurrency (rowversion / `IsConcurrencyToken`)** — and the task no longer asks for it. The API surface is **read + create only**: there is no `UPDATE` anywhere in scope, so a concurrency token guards against a write conflict that **cannot happen**. Demanding it was the rubric contradicting its own YAGNI rule — rewarding a pattern with no variation point to justify it. Adding one now counts as gold-plating (cat. 2).

**Quality signals**
- The schema evolves via migrations, never manually.
- Predictable, indexed queries; integrity guaranteed by the database.
- Connection pooling and timeouts configured.

📚 **Consensus basis:** Relational design best practices (normalization 1NF→3NF/BCNF, referential integrity, workload-oriented indexing). See the database design sources.

🤖 **Automated evaluation**
- **Method:** Roslyn reads the data layer — versioned **migrations** vs `EnsureCreated`, FK/relationship configuration, `HasIndex`, a concurrency token, `AsNoTracking` on read paths. The schema is then **exercised for real**: the live contract oracle creates cards and transactions against the actual Postgres the submission booted, so migrations that don't apply, a missing FK or a broken mapping surface as failed contract checks (a 500 instead of a 201) rather than as a static opinion.
- **Metric / threshold:** schema evolves via **migrations**, not `EnsureCreated` (`EnsureCreated` = half credit at best); referential integrity configured; **indexes** on FK/filter columns; `AsNoTracking` on reads.
- **Tools (local):** **Roslyn** + the live contract oracle against the running Postgres. *`EXPLAIN ANALYZE`/pg_stat_statements, SchemaCrawler and sqlfluff are retired: the submissions use EF Core migrations (there is no `.sql` to lint), and the N+1/seq-scan signals never discriminated between submissions at this data volume.*

---

## 6. Messaging 🟢 — 13%

Evaluates asynchronous integration via a broker (Kafka). Contributes to *Reliability* and *Compatibility*.

> **Scope: producer-only (the essence).** This benchmark asks for the *publishing* side only — on a
> successful transaction create, a durable producer publishes the event to the `transactions` topic,
> keyed by id. A **consumer, Transactional Outbox, and dead-letter path are explicitly out of scope**
> and are **not scored** (building them is unrequested gold-plating). What we verify is that the model
> can wire a broker and publish reliably.

**What to look for**
- A Kafka client is present and a real publish call runs on the successful create (`Produce`/`ProduceAsync`).
- The event lands on the `transactions` topic, keyed by the transaction **id** (same tx → same partition).
- A **durable** producer config (`Acks.All` / `EnableIdempotence`) so an acknowledged publish is not silently lost.
- Publish is **decoupled from the request's success**: a broker hiccup is caught-and-logged, **not** turned into a 500 after the row is already persisted.

**Quality signals**
- The producer is configured for durability, not fire-and-forget with default acks.
- A broker outage degrades gracefully (the write still succeeds) instead of failing the API.

📚 **Consensus basis:** Confluent/Apache Kafka — Producer durability (`acks=all`, idempotence) & Delivery Semantics.

🤖 **Automated evaluation**
- **Method:** static (Roslyn) detects the client, the publish call and the durable config. In `--deep`, the harness attaches its **own** consumer to the `transactions` topic and confirms a real event was published for a just-created transaction, keyed by id.
- **Metric / threshold:** client present + publish call present + durable config present (static); and, live, ≥1 event observed on `transactions` with `key == id` within the timeout.
- **Tools (local):** the harness `kafka-check` consumer (**kcat**) tailing the live topic. *That live observation is the proof, which is why the submission is **not** asked for a Testcontainers-Kafka test of its own (the task rules Testcontainers out entirely — see cat. 1).*

---

## 7. Security 🟠 — 14%

Evaluates the protection of the application, its data, and its integrations (ISO 25010's *Security*).

**What to look for**
- Authentication and authorization — *optional and **not scored** in this task: there is no user or ownership model in scope, so there is no **BOLA** (OWASP API #1) to test. If present it is mentioned in a report note; its absence is not a finding. (It used to be emitted as a **zero-weight metric** — a report line pretending to be a measurement, incapable of moving the score under any input. If a thing is worth reporting but cannot be scored, it is a note.)*
- Secrets management (no *hardcoded* credentials; env vars / secret manager).
- Validation and sanitization of **all** inputs (injection); beware of *mass assignment* / *excessive data exposure* (OWASP API #3).
- **Unrestricted Resource Consumption (OWASP API #4)** → rate limiting / throttling.
- Security Misconfiguration (#8), SSRF (#7), Unsafe Consumption of APIs (#10).
- Sensitive data at rest. *(Data in transit is out of scope here: the benchmark serves the API over plain HTTP on `:8080` — TLS terminates upstream — so TLS/HSTS is **not** required and **not** scored.)*
- Principle of least privilege (database, broker, services).
- Logs without leaking sensitive data (passwords, tokens, PII).

**Quality signals**
- No secrets in the repository or in logs; *deny by default*.
- **Domain compliance — PCI DSS Requirement 3 (card data):** PAN protected by strong encryption / truncation (at most first 6 + last 4) / tokenization / hashing; **sensitive authentication data (full track, CVV/CVC, PIN) never stored post-authorization**; cryptographic key management.

📚 **Consensus basis:** OWASP API Security Top 10 (2023); ISO/IEC 25010 — *Security* (confidentiality, integrity, authenticity, accountability, non-repudiation); PCI DSS v4.x Requirement 3.

🤖 **Automated evaluation**
- **Method:** the **PCI checks** are the core, and they run over the Roslyn AST, not over regex on raw text: every string literal in *production* source (test fixtures excluded) plus every `appsettings` value is Luhn-checked for an embedded **PAN**, and identifiers are searched for forbidden **sensitive authentication data** (`cvv`/`cvc`/track/PIN). Alongside: a **secret scan** of the tree and the **NuGet vulnerability graph**. Input validation and rate limiting are detected in the source.
- **Metric / threshold:** **0** Luhn-valid PANs in production code/config (PCI DSS Req. 3); **0** CVV/track/PIN fields; **0** secrets (gitleaks); **0 High/Critical** vulnerable packages (`dotnet list package --vulnerable --include-transitive`); input validation present; rate limiting present.
- **Tools (local):** **gitleaks**, `dotnet list package --vulnerable`, **Roslyn**. *OWASP ZAP (DAST), Semgrep (SAST) and Trivy (SCA) are retired: ZAP added 3–5 minutes per run to emit a near-identical alert list for every submission; Semgrep `--config auto` pulled a remote rule set that drifts (so the same source could score differently on two days); Trivy's NuGet findings duplicate `dotnet list --vulnerable`. **The BOLA scenario is also gone** — auth is optional in this task and there is no user/ownership model to violate.*

---

## 8. Resilience and Error Handling 🟢 — 4%

Evaluates behavior under failure (*Reliability*: fault tolerance, recoverability).

> **Why only 4%.** Most of what this category used to claim is now measured where it is *proved* rather than *declared*: the executability gate already caps any submission whose `/health` never answers (so `health-checks` was the third static copy of a signal the gate decides), and the live oracle already fires a malformed request and asserts a clean 4xx with no stack trace. What is left here — that the retry/timeout policies and the graceful shutdown are actually **wired** — is a static, declarative signal, and it is weighted like one.

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
- **Method:** Roslyn detects the resilience policies (Polly / `AddStandardResilienceHandler`), the health checks, the single global exception handler, graceful shutdown and timeout/cancellation propagation. Live, the contract oracle **sends a deliberately malformed request** and asserts the answer is a clean 4xx — not a 500, and with no stack trace, `Npgsql.`/`EntityFrameworkCore` internals or `.cs:line` markers in the body.
- **Metric / threshold:** a malformed request returns 4xx **without leaking internals**; retry/timeout/circuit-breaker policies present at the external I/O points (DB, broker); a single global exception handler; graceful shutdown wired.
- **Tools (local):** **Roslyn** + the live contract oracle. *Toxiproxy fault injection is retired: it needed a proxy sidecar in front of Postgres and Kafka plus an outage-and-recover cycle per run — minutes of wall-clock for a signal the static policy detection already approximates.*
- **Retired metric:** `health-checks`. Health was being counted **three times** — statically here, statically again in Observability, and by the live `/health` probe — on top of the executability gate that already caps a submission whose `/health` never answers at **1.5/5**. It was the most over-counted signal in the rubric and the one least able to change an outcome.

---

# The informational categories (9–11) — measured, reported, not scored

The three categories below are still evaluated on every run and printed in full in each report. They
carry **weight 0**: they do not move the weighted score and they are excluded from the coverage
denominator. This is deliberate, and it is the honest version of what was already true — at 1–4% each,
none of them could ever separate two submissions, and each duplicated a signal that something stronger
already decides (the executability gate, or the live oracle). Reporting them is useful; **ranking on them
was theatre.**

---

## 9. Observability 🟢 — *informational (weight 0)*

> *A means, not an ISO attribute.* Essential to operate the system — and still worth showing.

> **Scope: OpenTelemetry is not required, and neither is `/metrics`.** The task no longer asks for
> either; adding the OTel SDK is now counted as **gold-plating** (cat. 2). What the report shows is
> whether the service is *operable*: structured JSON logs, a correlation id, and `/health` answering on
> the running system.

**What to look for**
- **Structured (JSON) logging** with appropriate levels, and **no sensitive data in the logs** (the PAN rule is scored in Security).
- A request / **correlation id** propagated end to end.
- `/health` responding on the live system.

**Quality signals**
- Diagnose an incident without adding code.
- End-to-end correlation of an operation (HTTP → event).

📚 **Consensus basis:** Google SRE — *Four Golden Signals*; OpenTelemetry / CNCF — three pillars (logs, metrics, traces).

🤖 **Automated evaluation**
- **Method:** Roslyn confirms structured logging and the correlation id in the source; a live HTTP probe confirms `/health` answers 2xx on the running system.
- **Metric / threshold:** logs in **structured JSON** (Serilog / `AddJsonConsole`); a correlation/trace id propagated; `/health` responds **2xx live**.
- **Tools (local):** live HTTP probes + **Roslyn**. *The OTel SDK, an in-memory Collector and the trace-context assertion are retired along with the requirement itself.*
- **Why weight 0:** its decisive signal is `/health` — and that is the **executability gate**, which caps any submission that never answers it at **1.5/5** regardless of anything else here. The static `health-endpoint` and `metrics-endpoint` metrics are retired: the first was the third copy of the health signal, the second belonged to a requirement the task has dropped.

---

## 10. Portability, Configuration, and Deployment 🟢 — *informational (weight 0)*

Evaluates reproducibility and operation (*Portability*: installability, adaptability, replaceability), guided by 12-Factor.

**What to look for**
- **Configuration externalized in environment variables** (12-Factor III) — no secrets/config in the code.
- **Pluggable backing services** (database/broker as attached resources, swappable by config alone — 12-Factor IV).
- Reproducible build and **pinned** dependencies (12-Factor I/II).
- Containerization (Dockerfile running as **non-root**, docker-compose for dependencies).

**Quality signals**
- `clone → up → run` works on any machine.
- No environment configuration embedded in the code.

📚 **Consensus basis:** The Twelve-Factor App (Config, Backing services, Build/Release/Run); ISO/IEC 25010 — *Portability*. For **delivery** performance, a complementary reference: the **DORA metrics** — note these are *process* metrics, not assessable from a static code snapshot.

🤖 **Automated evaluation**
- **Method:** file checks for the Dockerfile, the compose, env-based config, dependency pinning and a non-root `USER`; **hadolint** lints the Dockerfile.
- **Metric / threshold:** the app starts reading **only env vars** (12-Factor III/IV); dependencies **pinned** (lock file / `global.json` / Central Package Management); non-root container; Dockerfile free of lint violations.
- **Tools (local):** **hadolint**; file/Roslyn checks. *dotnet-outdated and `act` are retired.*
- **Why weight 0:** whether the project actually deploys is **not a checklist item here** — the harness boots the submission's **own** `docker-compose.yml`, and the **executability gate** caps it at **1.0–1.5/5** if it never comes up healthy. Scoring "a Dockerfile exists" at 2% on top of a gate that already ran the thing was double-counting.
- **Retired metric:** `ci` (a CI workflow exists). **Nothing in this benchmark ever runs it** — the metric scored the existence of a YAML file, which is the definition of ceremony. What CI would have proven (it builds, its tests pass, it lints) the evaluator does itself, for real, on every run. The task no longer asks for a CI workflow.

---

## 11. Documentation 🟠 — *informational (weight 0)*

Evaluates support for understanding and operation (supports *Maintainability* and the developer's "Usability").

**What to look for**
- README with purpose, stack, setup, and run instructions.
- API documentation (an accessible OpenAPI/Swagger document).

**Quality signals**
- A new dev brings the project up following only the README.

📚 **Consensus basis:** ISO/IEC 25010 — *Maintainability/Usability*; contract convention via OpenAPI.

🤖 **Automated evaluation**
- **Method:** parse the README for its required sections (purpose, setup/prerequisites, how to run).
- **Metric / threshold:** README contains **purpose + setup + run**.
- **Tools (local):** markdown section parsing. *markdownlint, lychee and DocFX are retired: they graded README cosmetics and reachable third-party URLs — network-bound, flaky checks on a 1%-weight category.*
- **Why weight 0:** at 1%, the gap between a flawless README and **no README at all** moved the final score by **0.05** — less than the run-to-run noise of the same model on the same prompt. It was a number that looked like a measurement and could not act like one.
- **Retired metrics:** `api-docs` — the same package/invocation predicate as cat. 4's OpenAPI check, scoring one fact in two categories; the live `openapi-populated` there is the stronger form of it, and it *is* scored. `doc-comments` — `///` density measures typing, not engineering, and it is trivially gamed by a model that comments every property. The task no longer asks for XML docs.

---

## Scoring sheet (summary)

| # | Category | Weight | Score (0–5) | Weighted |
|---|-----------|------|------------|-----------|
| 1 | Functional Correctness & Tests | 20% | | |
| 2 | Architecture and Design | 12% | | |
| 3 | Code Quality | 10% | | |
| 4 | REST API Design | 14% | | |
| 5 | Persistence and Database | 13% | | |
| 6 | Messaging | 13% | | |
| 7 | Security | 14% | | |
| 8 | Resilience and Error Handling | 4% | | |
| | **Total** | **100%** | | |
| 9 | Observability | *informational* | | — |
| 10 | Portability, Configuration, and Deployment | *informational* | | — |
| 11 | Documentation | *informational* | | — |

> **Final score** = Σ (score × weight) over categories **1–8 only**. Categories 9–11 are measured and reported but carry no weight — they are excluded from the score *and* from the coverage denominator.
>
> **The executability gate still overrides everything.** However well a submission reads, the headline is capped by how far it actually got: source that does not compile **≤ 0.5**; compiles but ships no runnable system (no `docker-compose.yml`) **≤ 1.0**; has a compose but never boots healthy **≤ 1.5**. The run is the measurement.

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
