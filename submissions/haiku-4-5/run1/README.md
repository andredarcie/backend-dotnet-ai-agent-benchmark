# Credit Card REST API

A production-grade REST Web API for managing credit cards and transactions, built with .NET 10, ASP.NET Core, PostgreSQL, and Apache Kafka.

## Overview

This API provides endpoints for managing credit cards and their associated transactions. All data is persisted to PostgreSQL, and transaction events are published to Apache Kafka for downstream processing.

## Stack

- **.NET 10** with ASP.NET Core Web API (Controllers)
- **Entity Framework Core** with PostgreSQL (via Npgsql)
- **Apache Kafka** for event streaming (via Confluent.Kafka)
- **Docker Compose** for orchestration
- **xUnit** for testing
- **Serilog** for structured logging
- **Swagger/OpenAPI** for API documentation

## Prerequisites

- Docker and Docker Compose (for containerized deployment)
- .NET 10 SDK (for local development)

## Getting Started

### Quick Start with Docker

From the project root, run:

```bash
docker compose up --build
```

This command will:
1. Build the .NET API image
2. Start PostgreSQL (port 5432)
3. Start Kafka and Zookeeper (ports 9092, 29092, 2181)
4. Start the API (port 8080)
5. Run database migrations automatically

The API will be available at `http://localhost:8080`.

### Local Development

To run the API locally without Docker:

1. Ensure PostgreSQL is running on `localhost:5432`
2. Ensure Kafka is running on `localhost:9092`
3. From the `src/CreditCardApi` directory:

```bash
dotnet restore
dotnet build
dotnet run
```

## API Endpoints

### Health Check

```
GET /health
```

Returns the health status of the API.

**Response (200 OK):**
```json
{
  "status": "healthy"
}
```

### Credit Cards

#### Get All Credit Cards (Paginated)

```
GET /api/credit-cards?pageNumber=1&pageSize=10
```

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "cardholderName": "John Doe",
    "cardNumber": "****9010",
    "brand": "VISA",
    "creditLimit": 5000,
    "createdAt": "2026-01-01T12:00:00Z"
  }
]
```

#### Get Credit Card by ID

```
GET /api/credit-cards/{id}
```

**Response (200 OK):** Credit card object
**Response (404 Not Found):** Error details

#### Create Credit Card

```
POST /api/credit-cards
Content-Type: application/json

{
  "cardholderName": "John Doe",
  "cardNumber": "4532123456789010",
  "brand": "VISA",
  "creditLimit": 5000
}
```

**Response (201 Created):** Created card with ID
**Response (400 Bad Request):** Validation error

#### Update Credit Card

```
PUT /api/credit-cards/{id}
Content-Type: application/json

{
  "cardholderName": "Jane Doe",
  "creditLimit": 10000
}
```

**Response (200 OK):** Updated card
**Response (404 Not Found):** Card not found
**Response (400 Bad Request):** Validation error

#### Delete Credit Card

```
DELETE /api/credit-cards/{id}
```

**Response (204 No Content):** Success
**Response (404 Not Found):** Card not found

#### Get Transactions for Card

```
GET /api/credit-cards/{id}/transactions
```

**Response (200 OK):** List of transactions
**Response (404 Not Found):** Card not found

### Transactions

#### Get All Transactions (Paginated)

```
GET /api/transactions?pageNumber=1&pageSize=10
```

**Response (200 OK):** Array of transaction objects

#### Get Transaction by ID

```
GET /api/transactions/{id}
```

**Response (200 OK):** Transaction object
**Response (404 Not Found):** Error details

#### Create Transaction

```
POST /api/transactions
Content-Type: application/json

{
  "creditCardId": 1,
  "amount": 199.90,
  "merchant": "Amazon",
  "category": "Shopping"
}
```

**Response (201 Created):** Created transaction with ID
**Response (400 Bad Request):** Validation error

On successful creation, a message is published to the Kafka `transactions` topic with the transaction details.

#### Update Transaction

```
PUT /api/transactions/{id}
Content-Type: application/json

{
  "amount": 299.90,
  "merchant": "Best Buy"
}
```

**Response (200 OK):** Updated transaction
**Response (404 Not Found):** Transaction not found
**Response (400 Bad Request):** Validation error

#### Delete Transaction

```
DELETE /api/transactions/{id}
```

**Response (204 No Content):** Success
**Response (404 Not Found):** Transaction not found

## Data Model

### CreditCard

| Field          | Type    | Constraints |
|----------------|---------|-------------|
| id             | int     | Primary key, auto-increment |
| cardholderName | string  | Required, max 255 chars |
| cardNumber     | string  | Required, max 255 chars |
| brand          | string  | Optional, max 50 chars |
| creditLimit    | decimal | Required, >= 0 |
| createdAt      | datetime| UTC, set by server |

### Transaction

| Field        | Type    | Constraints |
|--------------|---------|-------------|
| id           | int     | Primary key, auto-increment |
| creditCardId | int     | Foreign key, required |
| amount       | decimal | Required, > 0 |
| merchant     | string  | Required, max 255 chars |
| category     | string  | Optional, max 100 chars |
| createdAt    | datetime| UTC, set by server |

## Configuration

The API reads configuration from environment variables:

| Variable | Default | Purpose |
|----------|---------|---------|
| `ConnectionStrings__PostgreSQL` | `Host=postgres;Port=5432;Database=creditcard;Username=postgres;Password=postgres` | Database connection string |
| `Kafka__BootstrapServers` | `kafka:9092` | Kafka broker addresses |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Execution environment |

## Kafka Integration

When a transaction is successfully created, an event is published to the `transactions` Kafka topic:

- **Topic:** `transactions`
- **Key:** Transaction ID (as string)
- **Value:** Transaction DTO as JSON (camelCase)

Example message:
```json
{
  "id": 1,
  "creditCardId": 1,
  "amount": 199.90,
  "merchant": "Amazon",
  "category": "shopping",
  "createdAt": "2026-01-01T12:00:00Z"
}
```

## Testing

Run the test suite:

```bash
dotnet test tests/CreditCardApi.Tests/CreditCardApi.Tests.csproj
```

Test coverage includes:
- **Unit Tests:** Business rule validation
- **Integration Tests:** Full API endpoint testing
- **Validation Tests:** Input validation rules

## Security

- **Card Number Protection:** Card numbers are masked (only last 4 digits shown) in API responses
- **Configuration:** All sensitive data (connection strings, credentials) comes from environment variables
- **Input Validation:** All inputs are validated before processing
- **Error Handling:** Stack traces are not exposed to clients; errors returned as RFC 9457 Problem Details

## Database Migrations

Migrations are automatically applied on application startup via EF Core. To add a new migration:

```bash
dotnet ef migrations add <MigrationName> -p src/CreditCardApi
```

## OpenAPI/Swagger

API documentation is available at `http://localhost:8080/swagger/index.html` (in development mode).

The OpenAPI document is automatically generated from the code and XML comments on the controllers.

## Logging

The API uses Serilog for structured JSON logging. Logs include:
- Request/response details
- Database operations
- Kafka publish events
- Application errors

## Performance

- Async/await used throughout for non-blocking I/O
- Pagination on collection endpoints
- NoTracking queries for read operations
- Proper indexing on foreign keys and frequently filtered columns
- Connection pooling for database connections

## Production Considerations

- The API runs as a non-root user in the container
- Database migrations run automatically on startup
- Health checks are configured for all services
- Graceful shutdown is supported
- The API respects cancellation tokens throughout

## Troubleshooting

### Kafka connection failed
Ensure the Kafka broker is healthy:
```bash
docker compose logs kafka
```

### Database migration errors
Check PostgreSQL logs:
```bash
docker compose logs postgres
```

### API not starting
View detailed logs:
```bash
docker compose logs api
```

## License

This project is provided as-is for educational and production use.
