# Credit Card REST API

A production-grade REST Web API for managing credit cards and transactions, built with .NET 10, PostgreSQL, and Apache Kafka.

## Purpose

This API provides a complete backend for credit card management, including:
- CRUD operations for credit cards
- CRUD operations for transactions
- Real-time event publishing to Kafka on transaction creation
- Comprehensive error handling with RFC 9457 Problem Details responses
- Health checks and structured logging
- OpenAPI/Swagger documentation

## Prerequisites

- Docker and Docker Compose installed
- No additional installation required; everything runs in containers

## Quick Start

From the project root directory:

```bash
docker compose up --build
```

This command will:
1. Build the .NET 10 API image
2. Start PostgreSQL 16
3. Start Apache Kafka with Zookeeper
4. Run database migrations automatically
5. Start the API service

Wait for all services to be healthy (typically 15-30 seconds), then the API will be ready.

## API Access

- **API Base URL**: `http://localhost:8080`
- **OpenAPI Schema**: `http://localhost:8080/openapi/v1.json`
- **Health Check**: `http://localhost:8080/health`

## Stack

- **.NET 10** with ASP.NET Core Web API
- **Entity Framework Core 9.0** with PostgreSQL (Npgsql)
- **Apache Kafka** for event streaming
- **Serilog** for structured logging
- **xUnit** for testing with Testcontainers for integration tests
- **Polly** for resilience and retry policies
- **OpenAPI/Swagger** for API documentation

## Endpoints

### Health

- `GET /health` - Returns `{ "status": "healthy" }`

### Credit Cards

| Method | Route                                    | Description                              |
|--------|------------------------------------------|------------------------------------------|
| GET    | `/api/credit-cards`                      | List credit cards (paginated)            |
| GET    | `/api/credit-cards/{id}`                 | Get a credit card by ID                  |
| POST   | `/api/credit-cards`                      | Create a new credit card                 |
| PUT    | `/api/credit-cards/{id}`                 | Update a credit card                     |
| DELETE | `/api/credit-cards/{id}`                 | Delete a credit card                     |
| GET    | `/api/credit-cards/{id}/transactions`    | List transactions for a credit card      |

### Transactions

| Method | Route                      | Description                              |
|--------|----------------------------|------------------------------------------|
| GET    | `/api/transactions`        | List transactions (paginated)            |
| GET    | `/api/transactions/{id}`   | Get a transaction by ID                  |
| POST   | `/api/transactions`        | Create a new transaction                 |
| PUT    | `/api/transactions/{id}`   | Update a transaction                     |
| DELETE | `/api/transactions/{id}`   | Delete a transaction                     |

## Request/Response Examples

### Create Credit Card

**Request:**
```json
POST /api/credit-cards
Content-Type: application/json

{
  "cardholderName": "John Doe",
  "cardNumber": "4532015112830366",
  "brand": "VISA",
  "creditLimit": 5000.00
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "cardholderName": "John Doe",
  "cardNumber": "4532015112830366",
  "brand": "VISA",
  "creditLimit": 5000.00,
  "createdAt": "2026-01-01T12:00:00Z"
}
```

### Create Transaction

**Request:**
```json
POST /api/transactions
Content-Type: application/json

{
  "creditCardId": 1,
  "amount": 199.99,
  "merchant": "Amazon",
  "category": "Shopping"
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "creditCardId": 1,
  "amount": 199.99,
  "merchant": "Amazon",
  "category": "Shopping",
  "createdAt": "2026-01-01T12:00:00Z"
}
```

The transaction is automatically published to the Kafka `transactions` topic.

### Error Response

**Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "Validation failed",
  "status": 400,
  "detail": "amount must be > 0"
}
```

## Environment Variables

Configuration via environment variables (all have sensible defaults for Docker Compose):

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | See docker-compose.yml | PostgreSQL connection string |
| `Kafka__BootstrapServers` | `kafka:9092` | Kafka broker address (inside container) |
| `ASPNETCORE_ENVIRONMENT` | `Development` | ASP.NET Core environment |

## Architecture

The API follows a layered architecture:

- **Presentation Layer** (`Controllers/`) - HTTP endpoints
- **Application Layer** (`Application/Dto/`) - DTOs and business logic
- **Domain Layer** (`Domain/Entities/`) - Domain entities (no infrastructure dependencies)
- **Infrastructure Layer** (`Infrastructure/`) - Database, messaging, migrations

Key design principles:
- Dependencies point inward (domain → application → presentation)
- DTOs used for request/response (never expose EF entities)
- No N+1 queries (proper use of includes and AsNoTracking)
- Proper FK constraints and indexes
- Row versioning for concurrency control

## Database Schema

### CreditCard Table
- `id` (int, PK, auto-increment)
- `cardholderName` (varchar(255), required)
- `cardNumber` (varchar(19), required, unique)
- `brand` (varchar(50), optional)
- `creditLimit` (numeric(18,2), required)
- `createdAt` (timestamp with time zone, required)
- `RowVersion` (bytea, for concurrency)

### Transaction Table
- `id` (int, PK, auto-increment)
- `creditCardId` (int, FK, required)
- `amount` (numeric(18,2), required, > 0)
- `merchant` (varchar(255), required)
- `category` (varchar(100), optional)
- `createdAt` (timestamp with time zone, required)
- `RowVersion` (bytea, for concurrency)

**Indexes:**
- CreditCard.CardNumber (unique)
- Transaction.CreditCardId (FK)
- Transaction.CreatedAt

## Kafka Integration

On every successful transaction creation, an event is published to the `transactions` Kafka topic:

- **Topic**: `transactions`
- **Key**: Transaction ID (ensures same transaction always maps to same partition)
- **Value**: Transaction JSON in camelCase
- **Broker**: `kafka:9092` (inside Docker) or `localhost:29092` (from host)

Topics are auto-created on startup.

## Testing

Run the test suite:

```bash
dotnet test tests/CreditCardApi.Tests/CreditCardApi.Tests.csproj
```

The test suite includes:
- Integration tests with Testcontainers for PostgreSQL
- Black-box HTTP endpoint tests
- Validation and error-handling tests
- ~80% line coverage target

## Production Considerations

### Security
- No hardcoded secrets (all configuration via environment variables)
- Structured logging (no sensitive data logged)
- Input validation on all endpoints
- RFC 9457 Problem Details for error responses
- Non-root Docker user

### Resilience
- Database connection retries (Npgsql retry policy)
- Kafka producer with acks=all and retries
- Graceful shutdown handling
- Health checks for dependencies

### Observability
- Structured JSON logging via Serilog
- Request/response logging
- Database query logging (in Development)
- Health check endpoint

### Performance
- Async/await throughout (no sync-over-async)
- Pagination on collection endpoints
- AsNoTracking on read-only queries
- Proper use of indexes

## Deployment

To run on a different host or with custom configuration:

```bash
docker compose -f docker-compose.yml up --build \
  -e ConnectionStrings__DefaultConnection="Host=your-postgres-host;..." \
  -e Kafka__BootstrapServers="your-kafka-host:9092"
```

Or update environment variables in `docker-compose.yml` directly.

## Troubleshooting

### Database migration fails
- Check PostgreSQL is healthy: `docker compose logs postgres`
- Ensure `ConnectionStrings__DefaultConnection` is correct

### Cannot connect to Kafka
- Check Kafka is healthy: `docker compose logs kafka`
- From host, use `localhost:29092`; from container, use `kafka:9092`

### Port already in use
- Change ports in `docker-compose.yml`
- Or stop existing containers: `docker compose down`

## Documentation

Full OpenAPI specification available at `/openapi/v1.json` endpoint after API starts.
