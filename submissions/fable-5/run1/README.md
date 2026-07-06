# Credit Card API

A production-grade REST API for managing credit cards and their transactions, built with
**.NET 10 / ASP.NET Core**, persisted in **PostgreSQL** (EF Core + Npgsql), and integrated with
**Apache Kafka**: every successfully created transaction is published to the `transactions` topic
through a **transactional outbox**, and an idempotent consumer with a dead-letter path reads it
back.

## Quick start

Prerequisites: **Docker** (with Compose v2). Nothing else — the .NET SDK is only needed to run the
tests locally.

```bash
docker compose up --build
```

That single command builds the image and starts the API, PostgreSQL and Kafka (KRaft, no
ZooKeeper). The database schema is created automatically at startup via EF Core migrations, and
the Kafka topics are created idempotently before the API begins serving.

| What | Where |
|---|---|
| API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| OpenAPI document | http://localhost:8080/openapi/v1.json |
| Liveness / contract health | http://localhost:8080/health → `{"status":"healthy"}` |
| Readiness (DB + Kafka) | http://localhost:8080/health/ready |
| Prometheus metrics | http://localhost:8080/metrics |
| Kafka (from the host) | `localhost:29092` |
| Kafka (inside compose) | `kafka:9092` |

### Try it

```bash
# create a card
curl -s -X POST http://localhost:8080/api/credit-cards \
  -H "Content-Type: application/json" \
  -d '{"cardholderName":"Ada Lovelace","cardNumber":"4111111111111111","brand":"VISA","creditLimit":5000}'

# charge it — this also publishes an event to the "transactions" topic
curl -s -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"creditCardId":1,"amount":199.90,"merchant":"Amazon","category":"shopping"}'

# watch the events arrive (key = transaction id, value = the transaction as camelCase JSON)
docker compose exec kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server kafka:9092 --topic transactions --from-beginning \
  --property print.key=true
```

## API surface

All collection endpoints are paginated via `?page=` and `?pageSize=` (default 1/20, max page size
100); pagination metadata is returned in `X-Total-Count`, `X-Page`, `X-Page-Size` and
`X-Total-Pages` headers so bodies stay plain JSON arrays. Errors are returned as
**RFC 9457 problem details** (`application/problem+json`). The API is versioned (v1 is the
default; select explicitly with `?api-version=1.0` or the `X-Api-Version` header).

| Method | Route | Success | Errors |
|---|---|---|---|
| GET | `/api/credit-cards` | 200 array | — |
| GET | `/api/credit-cards/{id}` | 200 | 404 |
| POST | `/api/credit-cards` | 201 + `Location` | 400 |
| PUT | `/api/credit-cards/{id}` | 200 | 400, 404 |
| DELETE | `/api/credit-cards/{id}` | 204 (cascades to its transactions) | 404 |
| GET | `/api/credit-cards/{id}/transactions` | 200 array | 404 |
| GET | `/api/transactions` | 200 array | — |
| GET | `/api/transactions/{id}` | 200 | 404 |
| POST | `/api/transactions` | 201 + `Location` | 400 (bad fields **or** unknown `creditCardId`) |
| PUT | `/api/transactions/{id}` | 200 | 400, 404 |
| DELETE | `/api/transactions/{id}` | 204 | 404 |

## Architecture

Four projects with dependencies pointing strictly inward (the domain references nothing):

```
src/
  CreditCardApi.Domain           entities (CreditCard 1:N Transaction), PAN truncation rules
  CreditCardApi.Application      use-case services, DTOs + validation, repository/outbox abstractions
  CreditCardApi.Infrastructure   EF Core (DbContext, migrations, repositories), Kafka (producer,
                                 outbox dispatcher, idempotent consumer, topic bootstrap)
  CreditCardApi.Api              controllers, middleware, problem details, health checks, OpenAPI
tests/
  CreditCardApi.UnitTests        business rules against mocked abstractions (NSubstitute)
  CreditCardApi.IntegrationTests black-box tests of the full app against real PostgreSQL and
                                 Kafka containers (Testcontainers)
```

### Messaging: exactly the boring, reliable patterns

- **Transactional outbox** — `POST /api/transactions` writes the row *and* an outbox message in
  one database transaction; nothing is published for failed requests, and a crash can never lose
  an event or emit one for a rolled-back row.
- **Durable producer** — the background dispatcher publishes with `acks=all` and idempotence
  enabled, and marks the outbox row processed only after the broker acknowledges (at-least-once).
- **Idempotent consumer** — the service also consumes `transactions` into a processed-events
  ledger keyed by transaction id, so duplicate deliveries are no-ops; offsets are committed
  manually, strictly **after** processing.
- **Dead-letter path** — unparseable messages go straight to `transactions.dlq` (with
  `x-dead-letter-reason`); transient failures are retried in place and dead-lettered after five
  attempts.

### Security

- **PAN is write-only**: the card number is truncated to its last four digits at the service
  boundary; only `CardNumberLast4` is stored and responses return `**** **** **** 1234`. The full
  number is never persisted, logged, or echoed back. CVV/PIN/track data have no fields anywhere.
- No hardcoded secrets — all configuration comes from environment variables (the compose file
  ships overridable local-dev defaults).
- Input validation on every request DTO (data annotations + business rules in services), with
  bounds chosen so invalid input can never surface as a 5xx.
- Fixed-window **rate limiting** per client IP on the API endpoints (429 + `Retry-After`).
- Container runs as a **non-root** user; dependencies are centrally pinned and free of known
  vulnerabilities (the transitively vulnerable `Microsoft.OpenApi` 2.0.0 is explicitly overridden).
- TLS terminates at the ingress in production (HSTS is enabled outside development); the container
  itself deliberately serves plain HTTP on 8080 with **no HTTPS redirect**.

### Resilience

- Single global exception handler (`IExceptionHandler`) — clients get opaque RFC 9457 responses,
  never stack traces.
- Startup migration and topic creation retry with exponential backoff (**Polly**) until the
  database/broker accept connections; EF Core additionally runs with `EnableRetryOnFailure`.
- Optimistic concurrency on cards and transactions via PostgreSQL's `xmin` (conflicts → 409).
- Liveness (`/health`, `/health/live`) is dependency-free; readiness (`/health/ready`) checks
  PostgreSQL and Kafka. Background workers honor cancellation and flush the producer on shutdown.

### Observability

- **OpenTelemetry** traces and metrics (ASP.NET Core, HttpClient, Npgsql, runtime), exposed to
  Prometheus at `/metrics`; OTLP export switches on automatically when
  `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- Structured **JSON logs** on stdout with scopes.
- A **correlation id** (`X-Correlation-Id`, honored inbound or generated) is echoed on every
  response, attached to every log line, persisted with the outbox message, propagated as a Kafka
  header, and picked up by the consumer — end to end across the broker.

## Configuration

All settings come from environment variables (see `docker-compose.yml` for the wired-up values):

| Variable | Purpose | Default |
|---|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string | — (required) |
| `Kafka__BootstrapServers` | Kafka broker address | `kafka:9092` |
| `Kafka__TransactionsTopic` | Topic for transaction-created events | `transactions` |
| `Kafka__DeadLetterTopic` | Dead-letter topic | `transactions.dlq` |
| `Kafka__ConsumerGroupId` | Consumer group of the built-in consumer | `credit-card-api` |
| `Kafka__OutboxPollIntervalMs` | Outbox dispatcher poll interval | `1000` |
| `Kafka__OutboxBatchSize` | Max outbox messages per dispatch cycle | `50` |
| `RateLimiting__PermitLimit` | Requests allowed per window per IP | `1000` |
| `RateLimiting__WindowSeconds` | Rate-limit window length | `10` |
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Compose-level database settings | `creditcards` / `creditcards` / `local-dev-only` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Enables OTLP export when set | unset |

## Testing

```bash
dotnet test --collect:"XPlat Code Coverage"
```

- **Unit tests** (62): PAN truncation/masking, request validation rules (`amount > 0`, required
  fields, limits), pagination math, and the service business rules — including "no event for a
  failed create" and "the outbox message is staged in the same transaction".
- **Integration tests** (44, Testcontainers — needs Docker): black-box acceptance of every
  endpoint and error contract against real PostgreSQL and Kafka, the exact `/health` body, the
  camelCase wire format, pagination headers, FK cascade on delete, the Kafka event (key = id,
  payload = created transaction), the dead-letter path for poison messages, and 40 concurrent
  creates without a single 5xx.

Coverage is collected with Coverlet (`coverage.cobertura.xml` under `TestResults/`). CI
(`.github/workflows/ci.yml`) builds, lints (`dotnet format --verify-no-changes`), tests, and
builds the Docker image on every push/PR.

## Notable implementation decisions

- **Simplicity over machinery**: no MediatR/AutoMapper/generic repositories — plain services,
  hand-written mapping, and only the abstractions the tests and the layering actually need.
- Collections return **plain arrays** with pagination headers (the mandated response shape) rather
  than envelope objects.
- Monetary values are `numeric(18,2)`; request bounds and 2-dp rounding guarantee an amount can
  never overflow the column or round to a non-positive stored value.
- `DELETE` uses single-statement `ExecuteDelete`; reads are `AsNoTracking`; list queries are
  indexed (FK index on `Transactions.CreditCardId`, partial index on pending outbox rows).
