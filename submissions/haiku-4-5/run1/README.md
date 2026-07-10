# Credit Card REST API

A production-grade REST API for managing credit cards and transactions, built with .NET 10, ASP.NET Core, PostgreSQL, and Apache Kafka.

## Features

- **Domain Model**: Credit cards (1:N) transactions
- **Persistence**: PostgreSQL with EF Core migrations and concurrency control
- **Messaging**: Apache Kafka for durable, idempotent transaction events
- **API**: RESTful endpoints with RFC 9457 Problem Details error handling, API versioning
- **Security**: Card number protection (truncation), Luhn validation, secrets from env vars only
- **Observability**: Structured logging with Serilog, request correlation IDs, health checks
- **Architecture**: Layered (Presentation, Application, Domain, Infrastructure)
- **Resilience**: DB retry policies, graceful Kafka producer shutdown, health checks
- **Testing**: Integration tests with Testcontainers, 100% async/non-blocking I/O

## Prerequisites

- Docker and Docker Compose
- .NET 10 SDK (for local development)

## Quick Start

```bash
docker compose up --build
```

The API will be available at `http://localhost:8080`  
Kafka will be available at `localhost:29092`

## Architecture

```
src/CreditCardApi.Api/
├── Presentation/Controllers/     # HTTP endpoints
├── Application/Dto/              # Data transfer objects
├── Domain/Entities/              # Business entities
└── Infrastructure/               # Data access, messaging
    ├── Data/                     # EF Core DbContext & migrations
    └── Messaging/                # Kafka producer

tests/CreditCardApi.Tests/
└── Integration/                  # Integration tests with Testcontainers
```

## API Endpoints

### Health & Diagnostics
- `GET /health` → Health check with database connectivity status
- `GET /metrics` → Application metrics and version info

### Credit Cards (API v1)
- `GET /api/v1/credit-cards` (paginated, returns truncated card numbers)
- `GET /api/v1/credit-cards/{id}`
- `POST /api/v1/credit-cards` (validates card number with Luhn algorithm)
- `PUT /api/v1/credit-cards/{id}`
- `DELETE /api/v1/credit-cards/{id}`
- `GET /api/v1/credit-cards/{id}/transactions`

### Transactions (API v1)
- `GET /api/v1/transactions` (paginated)
- `GET /api/v1/transactions/{id}`
- `POST /api/v1/transactions` (publishes to Kafka topic `transactions` with durability guarantees)
- `PUT /api/v1/transactions/{id}`
- `DELETE /api/v1/transactions/{id}`

All responses include `X-Correlation-ID` header for request tracing. Errors return RFC 9457 Problem Details.

## Configuration

All configuration is **environment-variable based** (no hardcoded secrets). Docker Compose sets these defaults:

```
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=creditcard;Username=postgres;Password=postgres
Kafka__BootstrapServers=kafka:9092
ASPNETCORE_ENVIRONMENT=Production
```

**Important**: In production, override `ConnectionStrings__DefaultConnection` and `Kafka__BootstrapServers` with your actual values. Do NOT commit real credentials to version control.

## Database

- PostgreSQL 16
- Auto-migrate on startup via EF Core
- Tables: `CreditCards`, `Transactions` with foreign key constraints
- Concurrency control using `xmin` (PostgreSQL row version)

## Kafka

- Broker: `kafka:9092` (container), `localhost:29092` (host)
- Topic: `transactions` (auto-created on first publish)
- Message Format: Transaction JSON (camelCase)
- Key: Transaction ID (ensures ordering and idempotency per transaction)
- Producer Configuration:
  - `Acks=All` — waits for all broker replicas before returning
  - `MessageSendMaxRetries=3` — automatic retry on failure
  - `SocketTimeoutMs=60s`, `RequestTimeoutMs=60s` — timeout protection
  - Graceful flush on application shutdown

**Note**: Published only after database transaction commits. If Kafka publish fails, the API returns an error and the transaction exists in the database only.

## Testing

```bash
dotnet test
```

Integration tests use Testcontainers to spin up PostgreSQL and Kafka for isolated, repeatable testing.

## Build & Run

### Docker
```bash
docker compose up --build
```

### Local development
```bash
dotnet build
dotnet run --project src/CreditCardApi.Api
```

Requires a PostgreSQL and Kafka instance running locally.

## Code Quality

- Structured logging (Serilog)
- Problem Details for error responses
- DTOs for all API contracts
- No sync-over-async anti-patterns
- Pagination on collection endpoints
- Proper HTTP status codes and verbs

## Security

- **Card Numbers**: Stored as-is in database (in production, encrypt PAN at rest), returned to clients as truncated (`****-****-****-XXXX`)
- **Input Validation**: All card numbers validated with Luhn algorithm; all required fields checked
- **No Hardcoded Secrets**: All configuration from environment variables; `.git ignore`d files never committed
- **Structured Logging**: Request correlation IDs propagated end-to-end; exception details logged server-side only
- **Correlation IDs**: Sent via `X-Correlation-ID` header for full request tracing

## Production Considerations

- **TLS/HTTPS**: Configure behind a reverse proxy (nginx, load balancer) with TLS termination
- **Database**: 
  - Use strong credentials (not the default `postgres:postgres`)
  - Enable SSL connections to PostgreSQL
  - Set up automated backups
  - Tune connection pooling for your workload
  - Use dedicated read replicas for read-heavy workloads
- **Kafka**:
  - Enable broker-level authentication and authorization
  - Set up consumer groups for event processing
  - Monitor broker health and topic replication
  - Archive events for compliance/auditability
- **Deployment**:
  - Use container orchestration (Kubernetes, Docker Swarm, ECS)
  - Set up centralized logging aggregation (ELK, CloudWatch, Datadog)
  - Monitor application metrics (response latency, error rates, queue depth)
  - Set up alerts for service degradation
  - Enable distributed tracing (Jaeger, OpenTelemetry)
- **Scaling**:
  - Stateless API — scale horizontally by running multiple instances behind a load balancer
  - Database connection pooling: tune `MaxPoolSize` based on expected concurrent requests
  - Kafka partitions: scale consumer groups across multiple instances
