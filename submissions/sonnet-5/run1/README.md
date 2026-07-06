# Credit Card API

A production-grade REST API for managing credit cards and their transactions. Built with **.NET 10 /
ASP.NET Core**, persisted in **PostgreSQL** (EF Core + Npgsql), and integrated with **Apache Kafka**:
every successfully created transaction is published to the `transactions` topic through a
**transactional outbox**, and the service's own **idempotent consumer** reads it back with a
**dead-letter** path for poison messages.

## Quick start

Prerequisites: **Docker** (with Compose v2). The .NET 10 SDK is only needed to run the tests locally.

```bash
docker compose up --build
```

That single command builds the API image and starts the API, PostgreSQL, and Kafka (KRaft mode, no
ZooKeeper). The database schema is created automatically at startup via EF Core migrations, and the
`transactions` / `transactions.dlq` Kafka topics are created idempotently before the API starts serving.

| What | Where |
|---|---|
| API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| OpenAPI document | http://localhost:8080/openapi/v1.json |
| Liveness | http://localhost:8080/health → `{"status":"healthy"}` |
| Readiness (Postgres + Kafka) | http://localhost:8080/health/ready |
| Prometheus metrics | http://localhost:8080/metrics |
| Kafka (from the host) | `localhost:29092` |
| Kafka (inside compose) | `kafka:9092` |

### Try it

```bash
# create a card — the PAN is truncated to its last 4 digits before it is ever persisted
curl -s -X POST http://localhost:8080/api/credit-cards \
  -H "Content-Type: application/json" \
  -d '{"cardholderName":"Ada Lovelace","cardNumber":"4111111111111111","brand":"VISA","creditLimit":5000}'

# charge it — this also publishes an event to the "transactions" topic
curl -s -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"creditCardId":1,"amount":199.90,"merchant":"Amazon","category":"shopping"}'

# watch the event arrive (key = transaction id, value = the transaction as camelCase JSON)
docker compose exec kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 --topic transactions --from-beginning --property print.key=true
```

## API surface

All collection endpoints are paginated via `?page=` and `?pageSize=` (default 1/20, max page size 100);
pagination metadata is returned in `X-Total-Count`, `X-Page`, `X-Page-Size`, and `X-Total-Pages` headers
so response bodies stay plain JSON arrays. Errors are **RFC 9457 problem details**
(`application/problem+json`). The API is versioned — v1 is the default; select it explicitly with
`?api-version=1.0` or the `X-Api-Version` header.

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

Four projects, dependencies pointing strictly inward — the domain references nothing:

```
src/
  CreditCardApi.Domain           entities (CreditCard 1:N Transaction), the PAN-truncation policy
  CreditCardApi.Application      use-case services, DTOs + validation, repository/outbox/UoW abstractions
  CreditCardApi.Infrastructure   EF Core (DbContext, migrations, repositories), Kafka (producer, outbox
                                 dispatcher, idempotent consumer, topic bootstrap)
  CreditCardApi.Api              controllers, middleware, problem details, health checks, OpenAPI
tests/
  CreditCardApi.UnitTests        business rules against mocked abstractions (NSubstitute)
  CreditCardApi.IntegrationTests black-box tests of the full app against real PostgreSQL and Kafka
                                 (Testcontainers)
```

Controllers stay thin (map DTOs, call a service, translate the result to a status code); business
rules live in `CreditCardService`/`TransactionService`; `ICreditCardRepository`,
`ITransactionRepository`, `IUnitOfWork`, and `ITransactionEventPublisher` are the seams that let the
domain and application layers stay ignorant of EF Core and Kafka. No MediatR/AutoMapper/generic
repository — plain services and hand-written mapping, since there was never a second implementation to
justify the extra machinery.

### Messaging: the boring, reliable patterns

- **Transactional outbox** — `POST /api/transactions` writes the transaction row *and* an outbox row
  referencing it in one `SaveChanges` call (one database transaction). EF Core's own relationship
  fix-up resolves the transaction's generated id before the outbox row is inserted, so the outbox
  never needs a second round-trip. Nothing is staged for a failed request.
- **Durable producer** — a background `OutboxDispatcher` polls for unprocessed rows and publishes them
  with `acks=all` and idempotence enabled, marking a row processed only once the broker acknowledges it
  (at-least-once). Rows that fail past a configured attempt count are re-routed to the dead-letter topic
  instead of retried forever.
- **Idempotent consumer** — the service also runs `TransactionEventsConsumer`, which dedupes by
  transaction id against a `consumed_transaction_events` ledger before applying any effect, and commits
  offsets **only after** that effect is applied.
- **Dead-letter path** — messages that fail to deserialize, carry no valid transaction id, or fail
  processing are published to `transactions.dlq` with an `x-dead-letter-reason` header instead of
  blocking the partition.
- **Correlation id propagation** — `X-Correlation-Id` (inbound or generated) is echoed on the HTTP
  response, staged on the outbox row, carried as a Kafka header, and available to the consumer — the
  same id follows one transaction from the HTTP request through the broker to consumption.

### Security

- **The PAN is never stored or returned.** `CardNumberPolicy` truncates the submitted card number to
  its last 4 digits at the moment a `CreditCard` is constructed; only `CardNumberLast4` is persisted,
  and responses show `**** **** **** 1234`. There is no CVV/PIN/track-data field anywhere in the model.
- No hardcoded secrets — all configuration comes from environment variables (`docker-compose.yml` ships
  overridable local-dev defaults, clearly not production credentials).
- Every request DTO is validated (data annotations + a custom "required, not just non-null" attribute),
  with bounds chosen so invalid input can never reach the database as a malformed or overflowing value.
- Fixed-window **rate limiting** per client IP (429 + `Retry-After` on the response).
- The container runs as a **non-root** user (the base image's pre-provisioned `app` account); dependency
  versions are centrally pinned, including an explicit override of the transitively-pulled vulnerable
  `Microsoft.OpenApi` 2.0.0.
- TLS terminates at the ingress in production (HSTS is enabled outside `Development`); the container
  itself deliberately serves plain HTTP on 8080 with **no HTTPS redirect**, so `localhost:8080` stays
  reachable.

### Resilience

- A single global exception handler (`IExceptionHandler` + `IProblemDetailsService`) — clients get
  opaque RFC 9457 responses; only the known application exceptions (not-found, business-rule violation,
  concurrency conflict) leak their message, everything else becomes a generic 500.
- Startup migration and Kafka topic creation retry with exponential backoff (**Polly**) until Postgres
  and the broker accept connections; EF Core's Npgsql provider additionally retries transient failures
  on every query (`EnableRetryOnFailure`).
- **Optimistic concurrency** on both entities via Postgres' `xmin` system column — a lost update between
  two concurrent writers surfaces as a 409, not a silent overwrite.
- `/health` (liveness) is dependency-free and always returns `{"status":"healthy"}`; `/health/ready`
  checks Postgres and Kafka. The outbox dispatcher and consumer both honor cancellation for a clean
  shutdown, and the shared Kafka producer flushes on disposal.

### Observability

- **OpenTelemetry** traces and metrics (ASP.NET Core, HttpClient, the .NET runtime, and Npgsql's own
  `ActivitySource`), scraped by Prometheus at `/metrics`; OTLP export switches on automatically when
  `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- Structured **JSON logs** on stdout, with the correlation id attached as a logging scope.
- `/health`, `/health/ready`, and `/metrics` for diagnosing the system without adding code.

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
| `Kafka__MaxDeliveryAttempts` | Failed publish attempts before dead-lettering | `5` |
| `RateLimiting__PermitLimit` | Requests allowed per window per IP | `1000` |
| `RateLimiting__WindowSeconds` | Rate-limit window length (seconds) | `10` |
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Compose-level database settings | `creditcards` / `creditcards` / `local-dev-only` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Enables OTLP export when set | unset |

## Testing

```bash
dotnet test --collect:"XPlat Code Coverage"
```

- **Unit tests** (76): PAN truncation, domain entity invariants (amount `> 0`, non-blank required
  fields, non-negative credit limit), request validation rules, pagination math, and the service layer
  — including "no card lookup call is wasted" and "the event is only staged when the create actually
  succeeds."
- **Integration tests** (32, Testcontainers — needs Docker): black-box acceptance of every endpoint and
  error contract against real PostgreSQL and Kafka containers, the exact `/health` body, the camelCase
  wire format, pagination headers, FK cascade on delete, 40 concurrent transaction creates without a
  single server error, and the full Kafka story — the outbox event lands on `transactions` with the
  right key/payload/correlation header, the built-in consumer records it exactly once, and a poison
  message lands on `transactions.dlq` with a reason header.

Combined line coverage is **~81%** (Coverlet + ReportGenerator). CI (`.github/workflows/ci.yml`) builds,
lints (`dotnet format --verify-no-changes`), runs the full test suite, and then boots the whole compose
stack and smoke-tests it, on every push/PR.

## Notable implementation decisions

- **Simplicity over machinery**: no MediatR/AutoMapper/generic repositories — plain services,
  hand-written mapping, and only the abstractions the layering and tests actually need.
- Collections return **plain arrays** with pagination headers rather than an envelope object, matching
  the response shape the spec calls for.
- Monetary values are `numeric(18,2)`; request bounds guarantee an amount can never overflow the column.
- Table and column names are snake_case (`EFCore.NamingConventions`) for a schema that reads naturally
  in `psql`; reads use `AsNoTracking()` and the one list query with a real access pattern (transactions
  by card, and the outbox dispatcher's pending-row scan) has a supporting index.
- `DELETE` cascades at the database level (`ON DELETE CASCADE`), not via application-side cleanup code.
