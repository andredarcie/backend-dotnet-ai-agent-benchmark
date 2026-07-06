# Credit Card API

Production-oriented REST API for managing credit cards and transactions. The service uses ASP.NET Core controllers on .NET 10, EF Core migrations against PostgreSQL, and Kafka-backed transaction events through a transactional outbox.

## Stack

- .NET 10 / ASP.NET Core Web API controllers
- EF Core + Npgsql + PostgreSQL
- Apache Kafka via Confluent.Kafka
- Transactional outbox, Kafka producer, idempotent consumer, dead-letter topic
- RFC 9457 Problem Details, API versioning, Swagger/OpenAPI
- Serilog JSON logs, OpenTelemetry traces/metrics, Prometheus `/metrics`
- xUnit, Coverlet, Testcontainers for PostgreSQL/Kafka acceptance tests

## Prerequisites

- Docker with Docker Compose
- Optional for local development outside Docker: .NET SDK 10.0.203+

## Run

From the repository root:

```bash
docker compose up --build
```

To avoid local port conflicts, override host ports:

```powershell
$env:API_PORT = "18080"
$env:KAFKA_EXTERNAL_PORT = "39092"
$env:POSTGRES_PORT = "15432"
docker compose up --build
```

The API listens on `http://localhost:8080`.

Useful endpoints:

- `GET http://localhost:8080/health`
- `GET http://localhost:8080/health/live`
- `GET http://localhost:8080/health/ready`
- `GET http://localhost:8080/metrics`
- `GET http://localhost:8080/swagger`

Kafka is available as `kafka:9092` inside Docker and `localhost:29092` from the host. Transactions are published to the `transactions` topic with the transaction id as the message key.

## Configuration

All runtime configuration is read from environment variables. Compose provides development defaults so the one-command boot works.

| Variable | Purpose | Default in compose |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=postgres;Port=5432;Database=creditcards;Username=creditcards;Password=...` |
| `Kafka__BootstrapServers` | Kafka bootstrap servers | `kafka:9092` |
| `Kafka__TransactionsTopic` | Transaction event topic | `transactions` |
| `Kafka__DeadLetterTopic` | Dead-letter topic | `transactions.dlq` |
| `Kafka__ConsumerGroupId` | Idempotent consumer group | `credit-card-api` |
| `Security__PanEncryptionKey` | Base64-encoded 32-byte AES key for PAN encryption | development key |
| `ASPNETCORE_URLS` | HTTP bind address inside the container | `http://+:8080` |
| `API_PORT` | Host port mapped to API container port 8080 | `8080` |
| `KAFKA_EXTERNAL_PORT` | Host port mapped to Kafka external listener | `29092` |
| `POSTGRES_PORT` | Host port mapped to PostgreSQL | `5432` |

For non-development deployments, provide a unique `Security__PanEncryptionKey` and database credentials through your secret manager or environment. The API does not store CVV, PIN, or track data. Card numbers are encrypted at rest and only masked values are returned.

Generate a local AES key:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

## API examples

Create a card:

```bash
curl -i -X POST http://localhost:8080/api/credit-cards \
  -H "Content-Type: application/json" \
  -d '{"cardholderName":"Ada Lovelace","cardNumber":"4111111111111111","brand":"VISA","creditLimit":5000}'
```

Create a transaction:

```bash
curl -i -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"creditCardId":1,"amount":199.90,"merchant":"Amazon","category":"shopping"}'
```

List transactions:

```bash
curl http://localhost:8080/api/transactions?page=1&pageSize=20
```

## Testing

Default local test run:

```bash
dotnet test CreditCardService.slnx --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

The default run executes unit tests and skips Docker-backed acceptance tests. To run PostgreSQL/Kafka integration tests:

```bash
$env:RUN_TESTCONTAINERS = "true"
dotnet test CreditCardService.slnx
```

CI builds, formats, tests with coverage, and audits NuGet vulnerabilities.

## Notes

- Schema is applied on startup with EF Core migrations, not `EnsureCreated()`.
- Collections are paginated with `page` and `pageSize`; pagination metadata is returned in `X-Page`, `X-Page-Size`, and `X-Total-Count` headers.
- Errors use `application/problem+json` Problem Details.
- The API intentionally does not redirect HTTP to HTTPS so `http://localhost:8080` remains reachable in containers; HSTS is configured for production HTTPS deployments.

