# Credit Card API

A production-grade REST API for credit cards and their transactions, built on **.NET 10 / ASP.NET
Core**, backed by **PostgreSQL**, and integrated with **Apache Kafka**. Every transaction created
through the API is published as an event to a Kafka topic.

## Purpose

The service exposes a small, deliberately narrow surface — list/read/create credit cards, and
list/read/create transactions against them — and holds that surface to a production bar:
layered architecture, encrypted PAN storage, structured logging, resilient Kafka/DB access,
RFC 9457 error responses, and a fast offline unit test suite.

## Stack

| Concern        | Choice                                                              |
|-----------------|----------------------------------------------------------------------|
| Runtime         | .NET 10 / ASP.NET Core (Controllers)                                |
| Persistence     | PostgreSQL via EF Core + Npgsql, schema managed by EF Core migrations |
| Messaging       | Apache Kafka via Confluent.Kafka                                    |
| Resilience      | Polly (Kafka publish retry/circuit-breaker/timeout), Npgsql retry-on-failure |
| Logging         | Serilog, structured JSON to console                                  |
| API docs        | Built-in `Microsoft.AspNetCore.OpenApi` + Scalar UI                  |
| Tests           | xUnit + Moq + Coverlet, unit-only, no Docker/DB/broker required      |
| Containers      | Multi-stage Dockerfile, non-root runtime user                        |

## Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose (to run the full stack)
- [.NET 10 SDK](https://dotnet.microsoft.com/) (only needed to build/test outside Docker)

## How to run

From the repository root:

```bash
docker compose up --build
```

This starts three containers — PostgreSQL, Kafka (KRaft, single node), and the API — with **no
manual steps**. On startup the API applies pending EF Core migrations (creating the schema) and
ensures the `transactions` Kafka topic exists, both with retry in case a dependency is still
coming up.

Once running:

- API: `http://localhost:8080`
- Kafka (from the host): `localhost:29092`
- OpenAPI document: `http://localhost:8080/openapi/v1.json`
- Interactive API docs (Scalar): `http://localhost:8080/scalar`

```bash
# health check
curl http://localhost:8080/health

# create a credit card
curl -X POST http://localhost:8080/api/credit-cards \
  -H "Content-Type: application/json" \
  -d '{"cardholderName":"Ada Lovelace","cardNumber":"4111111111111111","brand":"VISA","creditLimit":5000.00}'

# create a transaction against it (creditCardId from the response above)
curl -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"creditCardId":1,"amount":199.90,"merchant":"Amazon","category":"shopping"}'
```

A successful transaction create publishes the transaction (camelCase JSON, keyed by its id) to the
`transactions` Kafka topic. To watch it land, from another terminal:

```bash
docker compose exec kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 --topic transactions --from-beginning
```

### Running without Docker

```bash
dotnet restore
dotnet build
dotnet test          # unit tests only - no external services needed
```

Running the API itself outside Docker still needs a reachable Postgres and Kafka broker; point it
at them with the environment variables below (e.g. `dotnet run --project src/CreditCardApi.Api`).

## Configuration (environment variables)

All configuration comes from environment variables - nothing is hardcoded. `docker-compose.yml`
sets these for local development; override them for any other deployment.

| Variable                          | Purpose                                              | Example (local dev)                                             |
|-------------------------------------|-------------------------------------------------------|-------------------------------------------------------------------|
| `ConnectionStrings__Default`      | Npgsql connection string                             | `Host=postgres;Port=5432;Database=creditcardapi;Username=...;Password=...` |
| `Kafka__BootstrapServers`         | Kafka broker address                                 | `kafka:9092`                                                     |
| `Kafka__TransactionsTopic`        | Topic the create-transaction event is published to  | `transactions`                                                   |
| `Security__PanEncryptionKey`      | Base64 32-byte (AES-256) key encrypting the PAN at rest | generate with `openssl rand -base64 32`                       |
| `ASPNETCORE_ENVIRONMENT`          | ASP.NET Core environment                             | `Production`                                                     |

> The key shipped in `docker-compose.yml` is a fixed development-only value. Generate a new one
> (`openssl rand -base64 32`) and inject it via your secret store for any non-local deployment.

## API surface

`GET /health` → `200 { "status": "healthy" }`. `GET /health/ready` reports DB/Kafka connectivity.

| Method | Route                                 | Notes                                                    |
|--------|----------------------------------------|-----------------------------------------------------------|
| GET    | `/api/credit-cards`                   | Paginated (`pageNumber`, `pageSize`, default 1/20, max 100) |
| GET    | `/api/credit-cards/{id}`              | 404 if missing                                            |
| POST   | `/api/credit-cards`                   | 201 + `Location`; 400 if `cardholderName`/`cardNumber` empty |
| GET    | `/api/credit-cards/{id}/transactions` | 404 if the card doesn't exist                              |
| GET    | `/api/transactions`                   | Paginated                                                  |
| GET    | `/api/transactions/{id}`              | 404 if missing                                             |
| POST   | `/api/transactions`                   | 201 + `Location`; 400 if `merchant` empty, `amount` <= 0, or `creditCardId` doesn't exist |

There is no `PUT`/`DELETE` - the surface is read + create only, by design. Errors are
[RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) Problem Details (`application/problem+json`).

## Architecture

Four projects, dependencies pointing inward:

```
CreditCardApi.Domain          entities only, no framework/infra references
CreditCardApi.Application     DTOs, repository/publisher interfaces, use-case services, validation
CreditCardApi.Infrastructure  EF Core, Kafka producer, PAN encryption, health checks
CreditCardApi.Api             Controllers, Program.cs, middleware
```

Controllers depend on `Application` service interfaces only; `Infrastructure` implements those
interfaces and is wired up in `Program.cs`. Business rules (required fields, `amount > 0`, FK
existence) live in the `Application` services, not in controllers or EF configuration, so they're
covered by fast, isolated unit tests.

## Security

- **No hardcoded secrets** - connection string, Kafka broker, and the PAN encryption key are all
  environment variables.
- **PAN protection** - the card number is encrypted at rest with AES-256-GCM (a fresh random nonce
  per value, via an EF Core value converter) and is **never** returned or logged unmasked; API
  responses only ever show a masked form (`**** **** **** 1111`). No CVV/PIN/track data is
  collected or stored - it isn't part of the domain model.
- **Validation** on every write path (business rules in `Application`, malformed JSON rejected by
  the framework before it reaches a controller).
- **Rate limiting** - a global fixed-window limiter (100 requests / 10s per client IP).
- Dependencies are checked for known vulnerabilities (`dotnet list package --vulnerable`); none
  are used.
- Plain HTTP on `:8080` - TLS terminates upstream in this deployment, so there's no HTTPS
  redirect/HSTS configuration to fight it.

## Resilience

- A single global exception handler (`IExceptionHandler`) maps known exceptions to Problem
  Details and logs everything else as a 500 - no stack trace ever reaches a client.
- `/health` (liveness) and `/health/ready` (DB + Kafka connectivity) are separate endpoints.
- EF Core's Npgsql provider retries transient failures; startup migration and Kafka topic creation
  each retry with backoff while a dependency is still coming up.
- The Kafka producer publishes with `acks=all` + idempotence + broker-side retries, wrapped in a
  Polly retry/circuit-breaker/timeout pipeline. If publishing still fails, the failure is logged
  and swallowed - the transaction row is already committed, so the request still returns 201.

## Observability

Structured JSON logs (Serilog, compact format) to the console. Every request carries an
`X-Correlation-Id` (reused if the caller sent one, minted otherwise), echoed on the response and
attached to every log line written while handling that request.

## Testing

```bash
dotnet test
```

72 unit tests (xUnit + Moq), covering the `Application` business rules (required fields,
`amount > 0`, FK existence, pagination clamping), PAN masking/encryption, the Kafka publisher's
retry-then-swallow behavior, the global exception handler's status-code mapping, and the EF Core
model configuration (indexes, FK delete behavior, column types - built in-memory, no connection
opened). No test needs Docker, a database, or a broker; the suite runs in-process in about a
second. Coverage is collected with Coverlet (`coverlet.runsettings` excludes EF's generated
migration code):

```bash
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

The `CreditCardApi.Application` layer - where the business rules live - is covered above 95%;
the overall solution (including EF configuration, DI wiring, and Kafka/Postgres plumbing that
legitimately needs a live dependency to exercise) is above 60%.
