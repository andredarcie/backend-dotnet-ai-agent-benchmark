# Credit Card API (.NET 10)

A production-grade REST API for **credit cards** and **transactions**, backed by **PostgreSQL** and
**Apache Kafka**. The whole system starts with a single command:

```bash
docker compose up --build
```

That brings up the API, PostgreSQL and Kafka, applies EF Core migrations automatically, creates the
Kafka topics, and exposes:

| What | Where |
|------|-------|
| API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| Liveness contract | http://localhost:8080/health → `{ "status": "healthy" }` |
| Liveness / readiness probes | `/health/live`, `/health/ready` |
| Prometheus metrics | http://localhost:8080/metrics |
| Kafka (host) | `localhost:29092` · (in-network) `kafka:9092` |

## Stack

- **.NET 10 / ASP.NET Core** Web API (controllers)
- **EF Core 10** + **Npgsql** (PostgreSQL) — schema created via **migrations** on startup
- **Confluent.Kafka** producer + consumer
- **OpenTelemetry** (traces + metrics), structured **JSON** logs
- **Polly** for resilience, built-in **rate limiting**, **Swashbuckle** OpenAPI

## Architecture

A clean, layered solution with dependencies pointing inward (Domain depends on nothing):

```
src/
  CreditCardApi.Domain          # entities + invariants (no infrastructure deps)
  CreditCardApi.Application      # use cases, DTOs, abstractions (repos, UoW, events, PAN, clock)
  CreditCardApi.Infrastructure  # EF Core, repositories, Kafka, outbox, consumer, PAN encryption
  CreditCardApi.Api             # controllers, Program, middleware, cross-cutting concerns
tests/
  CreditCardApi.UnitTests        # domain rules, validation, services (fakes)
  CreditCardApi.IntegrationTests # black-box tests over real Postgres + Kafka (Testcontainers)
```

Controllers are thin: they delegate to application services and translate results to HTTP. Entities are
never exposed — every request/response uses a **DTO**, and collections are **paginated**.

## API surface

`GET /health` → `200 { "status": "healthy" }`

### `api/credit-cards`
| Method | Route | Success | Errors |
|--------|-------|---------|--------|
| GET | `/api/credit-cards` | 200 (paged) | — |
| GET | `/api/credit-cards/{id}` | 200 | 404 |
| POST | `/api/credit-cards` | 201 + `Location` | 400 |
| PUT | `/api/credit-cards/{id}` | 204 | 400, 404 |
| DELETE | `/api/credit-cards/{id}` | 204 | 404 |
| GET | `/api/credit-cards/{id}/transactions` | 200 (paged) | 404 |

### `api/transactions`
| Method | Route | Success | Errors |
|--------|-------|---------|--------|
| GET | `/api/transactions` | 200 (paged) | — |
| GET | `/api/transactions/{id}` | 200 | 404 |
| POST | `/api/transactions` | 201 + `Location` | 400 |
| PUT | `/api/transactions/{id}` | 204 | 400, 404 |
| DELETE | `/api/transactions/{id}` | 204 | 404 |

Errors are returned as **RFC 9457 Problem Details** (`application/problem+json`). Pagination uses
`?page=` and `?pageSize=` (default 20, max 100). API versioning is enabled (default `v1`, also selectable
via the `X-Api-Version` header or `?api-version=`).

Example — `POST /api/transactions`:

```json
{ "creditCardId": 1, "amount": 199.90, "merchant": "Amazon", "category": "shopping" }
```

→ `201 Created`

```json
{ "id": 1, "creditCardId": 1, "amount": 199.90, "merchant": "Amazon", "category": "shopping", "createdAt": "2026-01-01T12:00:00Z" }
```

## Events (Kafka)

On every successful `POST /api/transactions`, the created transaction (camelCase JSON) is published to
the **`transactions`** topic, keyed by the transaction id. Delivery uses the **Transactional Outbox**
pattern: the transaction row and an outbox row are committed in one database transaction, then a
background dispatcher publishes to Kafka with an idempotent, `acks=all` producer and retries.

A background **consumer** reads the topic as a downstream projection: it is **idempotent** (deduplicates
by message key), commits offsets **only after** processing, and routes poison messages to a
**dead-letter** topic (`transactions.DLQ`).

## Security

- **No hardcoded secrets** — all configuration comes from environment variables.
- The **PAN** is encrypted at rest with **AES-256-GCM** and never logged or returned; responses show
  only a masked value (`**** **** **** 1234`). CVV / PIN / track data are **never** accepted or stored.
- Input is validated at the edge (DataAnnotations → `400` Problem Details) and re-validated by domain
  invariants.
- Per-client **rate limiting**, HSTS in production. The container intentionally serves **HTTP on 8080**
  (no forced HTTPS redirect) so it stays reachable at `http://localhost:8080`.

## Configuration (environment variables)

| Variable | Default | Purpose |
|----------|---------|---------|
| `ConnectionStrings__Default` | — (required) | PostgreSQL connection string |
| `Kafka__BootstrapServers` | `kafka:9092` | Kafka broker address |
| `Kafka__TransactionsTopic` | `transactions` | Event topic |
| `Kafka__DeadLetterTopic` | `transactions.DLQ` | Dead-letter topic |
| `Kafka__EnableConsumer` | `true` | Run the in-process projection consumer |
| `Outbox__PollIntervalSeconds` | `5` | Outbox dispatch interval |
| `Security__PanEncryptionKey` | (ephemeral) | base64 32-byte AES key; set to keep PANs decryptable across restarts |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | (unset) | Enables OTLP export of traces/metrics/logs |

## Running locally (without Docker)

```bash
# needs a local Postgres + Kafka reachable; then:
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=creditcards;Username=postgres;Password=postgres"
export Kafka__BootstrapServers="localhost:29092"
dotnet run --project src/CreditCardApi.Api
```

## Tests

```bash
dotnet test tests/CreditCardApi.UnitTests          # fast, no Docker
dotnet test tests/CreditCardApi.IntegrationTests   # spins up Postgres + Kafka via Testcontainers (needs Docker)
```

Coverage is collected with **Coverlet** (`--collect:"XPlat Code Coverage"`), wired into CI
(`.github/workflows/ci.yml`: restore → format check → build → unit + integration tests).
