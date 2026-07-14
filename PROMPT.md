# Task: Credit Card REST API (.NET 10)

Build a **production-grade backend**: a REST Web API backed by **PostgreSQL** and integrated with
**Apache Kafka**. Everything lives in **one project folder** and starts with **one command** from that
folder: `docker compose up --build`.

**The bar.** This should read like a service you'd actually ship — *how* it's built matters as much as
whether the endpoints work. Architecture, code quality, persistence, messaging, security, resilience,
testing, and observability should all be evident **in the source**, not just at runtime. The functional
spec below is only the baseline; the real work is doing it well. And keep it **as simple as the
requirements allow** — no speculative layers, patterns, or abstractions; overengineering is a defect,
not a bonus.

---

## Part A — What to build (functional baseline)

### Stack
- **.NET 10 + ASP.NET Core** Web API (Controllers).
- **EF Core** + **PostgreSQL** (Npgsql).
- **Apache Kafka** (a .NET client, e.g. `Confluent.Kafka`).
- A root **`docker-compose.yml`** that starts **API + Postgres + Kafka**.
- Schema created **automatically on startup** — via **EF Core migrations** (not `EnsureCreated()`).
- All configuration comes from **environment variables** (no hardcoded connection strings/secrets).

### Domain model — 1:N (one `CreditCard` → many `Transaction`)

#### CreditCard
| Field            | Type      | Rules                                |
|------------------|-----------|--------------------------------------|
| `id`             | int       | PK, auto-increment                   |
| `cardholderName` | string    | **required**, non-empty              |
| `cardNumber`     | string    | **required**, non-empty — **sensitive** (see Security) |
| `brand`          | string?   | optional (e.g. `VISA`, `MASTERCARD`) |
| `creditLimit`    | decimal   | **required**, >= 0                   |
| `createdAt`      | datetime  | set by the server (UTC)              |

#### Transaction
| Field          | Type      | Rules                                                   |
|----------------|-----------|---------------------------------------------------------|
| `id`           | int       | PK, auto-increment                                      |
| `creditCardId` | int       | **required FK** → must reference an existing CreditCard |
| `amount`       | decimal   | **required**, must be **> 0**                           |
| `merchant`     | string    | **required**, non-empty                                 |
| `category`     | string?   | optional                                                |
| `createdAt`    | datetime  | set by the server (UTC)                                 |

JSON is **camelCase** throughout.

### API surface (listens on **8080**, in container and on host)

- `GET /health` → **200** `{ "status": "healthy" }`

`CreditCardsController` under `api/credit-cards`:
| Method | Route                                 | Success | Errors |
|--------|---------------------------------------|---------|--------|
| GET    | `/api/credit-cards`                   | 200 (array, paginated) | — |
| GET    | `/api/credit-cards/{id}`              | 200 | 404 if not found |
| POST   | `/api/credit-cards`                   | 201 (+`Location`, body with `id`) | 400 if `cardholderName`/`cardNumber` empty |
| GET    | `/api/credit-cards/{id}/transactions` | 200 (array) | 404 if card not found |

`TransactionsController` under `api/transactions`:
| Method | Route                    | Success | Errors |
|--------|--------------------------|---------|--------|
| GET    | `/api/transactions`      | 200 (array, paginated) | — |
| GET    | `/api/transactions/{id}` | 200 | 404 if not found |
| POST   | `/api/transactions`      | 201 (+`Location`, body with `id`) | 400 if `merchant` empty, `amount` <= 0, or `creditCardId` doesn't exist |

> The surface is **read + create only** — there is **no `PUT` and no `DELETE`** in scope. Don't add them.

Example — `POST /api/transactions` with `{ "creditCardId": 1, "amount": 199.90, "merchant": "Amazon", "category": "shopping" }` → **201**:
```json
{ "id": 1, "creditCardId": 1, "amount": 199.90, "merchant": "Amazon", "category": "shopping", "createdAt": "2026-01-01T12:00:00Z" }
```

### Events (Kafka)
On every **successful** transaction create (`POST /api/transactions` → 201), publish to Kafka:
- **Topic:** `transactions` · **Value:** the created transaction as JSON (camelCase) · **Key:** the
  transaction `id` as a string (so the same transaction always maps to the same key).
- Publish **after** the row is persisted, only for the successful create.
- **Networking:** broker reachable as `kafka:9092` inside Docker **and** `localhost:29092` from the host
  (set advertised listeners accordingly and publish port `29092`). Auto-create the topic on startup. The
  API reads the broker address from config (default `kafka:9092`, e.g. `Kafka__BootstrapServers`).

---

## Part B — Engineering standards

Treat this like code going to production and hold the whole codebase to that bar. Demonstrate each of the
following the **standard, idiomatic .NET way** — reach for the recognized artifact, not a shortcut. Don't
over-deliver either: build what the spec calls for and build it well. Items marked **optional** are
genuinely optional — skipping them is fine, and adding machinery for its own sake is worse than leaving
it out.

**Architecture & design** — clear layers (presentation / application / domain / data) with dependencies
pointing **inward** (domain references no infrastructure); thin controllers; no god classes; introduce
abstractions only where there's a real variation point (don't add an interface per class).

**Code quality** — idiomatic and readable; **no empty `catch`** (handle, rethrow, or at least log — a
bare comment still counts as swallowing); no dead code or `TODO`/`FIXME`; analyzers enabled via
`.editorconfig`; `dotnet format`-clean.

**REST API design** — correct verbs and status codes; return errors as **RFC 9457 Problem Details**
(`application/problem+json`); use **DTOs** in and out (never expose EF entities); paginate collections;
expose **OpenAPI/Swagger** whose document actually **describes every endpoint** (a served-but-empty spec
does not count).

**Persistence** — **EF Core migrations** (not `EnsureCreated`); FK constraints and **indexes** on FK /
filter columns; `AsNoTracking` on read paths; no N+1 queries.

**Messaging** — a **durable producer** (acks + retries) that publishes the transaction event **after**
the row is persisted. A broker hiccup must **not** fail the request: handle a publish failure
gracefully (catch-and-log — don't turn it into a 500 once the data is saved). *Publishing the event
reliably is the whole requirement here — there is **no consumer, outbox, or dead-letter** in scope; don't
build one.*

**Security** — no hardcoded secrets (env vars only); **protect the PAN** — encrypt, tokenize, or truncate
it, and never log it — and **never store CVV/PIN/track data**; validate all input; apply rate limiting;
keep dependencies free of known vulnerabilities. The API is served over plain HTTP on
`http://localhost:8080` (TLS terminates upstream in this deployment) — so **no TLS/HSTS config and no
HTTPS redirect**. Authentication/authorization is **optional**: there's no user or ownership model in
scope.

**Resilience** — a single **global exception handler** (no stack traces leak to clients); **liveness and
readiness** health checks; retries / timeouts / circuit breakers on Kafka and the DB (e.g. **Polly**);
graceful shutdown.

**Testing** — xUnit or NUnit, **unit tests only**. Cover the business rules that matter (the FK must
exist, `amount > 0`, required fields non-empty) with fast, isolated tests; collect coverage (**Coverlet**)
and aim for a **reasonable bar (~60%+)** on the code that matters — not a number for its own sake.
*No integration, end-to-end or acceptance tests* — **no Testcontainers, no `WebApplicationFactory`**, and
no test may need Docker, a database, or a broker. The suite must run **in-process, offline, in seconds**.

**Performance** — async, non-blocking I/O throughout (**no sync-over-async**: no `.Result`, `.Wait()`,
`.GetAwaiter().GetResult()`); a **stateless** API; pagination on collections.

**Observability** — **structured** JSON logs (and never log the PAN); a request / correlation id
propagated end to end. The `/health` endpoint is already in the API surface above.

**Portability & deploy** — a `Dockerfile` (running as a **non-root** user) and the `docker-compose.yml`;
config via env vars; **pin dependency versions** (a NuGet lock file, `global.json`, or Central Package
Management); one-command boot.

**Documentation** — a `README` covering **purpose**, **prerequisites/setup**, **how to run**, the stack,
and env vars.

> Standard fake test-card numbers used only inside the test project are normal and fine — they're test
> fixtures, not stored secrets.

---

## Out of scope — do NOT build these

The brief means what it says about simplicity: **shipping machinery nobody asked for is a defect, and it
is scored as one.** None of the following earns credit; each one costs you.

| Don't build | Why it's out of scope |
|-------------|------------------------|
| `PUT` / `PATCH` / `DELETE` endpoints | the surface is **read + create only** |
| A Kafka **consumer**, a **transactional outbox**, a **dead-letter** path | the requirement is to *publish* reliably — producer-only |
| **OpenTelemetry** (SDK, collector, exporters) | not required; structured logs + a correlation id is the bar |
| **API versioning** (`Asp.Versioning`, `/v1/` routing) | there is one version of one API |
| A **`/metrics`** endpoint | not required |
| **Testcontainers** / `WebApplicationFactory` / any integration or e2e test | the suite is **unit-only**, offline, in seconds |
| **Optimistic concurrency** (rowversion / `IsConcurrencyToken`) | nothing in scope ever updates a row — there is no write conflict to lose |
| A **CI workflow** | nothing here runs it |
| Auth / users / ownership | optional and unscored — there is no user model in scope |

Build the thing the brief asks for, and build *that* well.

---

## Deliverables (project root)

`docker-compose.yml` · `Dockerfile` · the layered .NET source (controllers, application/use-case layer,
data/repository layer, entities, `DbContext`, Kafka producer, DTOs) · a **test project** · a `README.md`.

`docker compose up --build` must bring everything up with **no manual steps**: API at
`http://localhost:8080`, Kafka at `localhost:29092`, and created transactions landing on the
`transactions` topic.
