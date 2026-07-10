# Credit Card REST API

A production-grade REST API for managing credit cards and transactions, built with .NET 10, ASP.NET Core, PostgreSQL, and Apache Kafka.

## Features

- **RESTful API** - Complete CRUD operations for credit cards and transactions
- **PostgreSQL** - Persistent storage with EF Core migrations
- **Apache Kafka** - Event-driven architecture with transactional outbox pattern
- **Structured Logging** - Serilog for JSON-formatted logs
- **Error Handling** - RFC 9457 Problem Details responses
- **Pagination** - Support for paginated collection endpoints
- **Docker** - Full containerization with docker-compose for single-command startup
- **OpenAPI** - Swagger documentation

## Architecture

```
Controllers
   ↓
Services (Business Logic)
   ↓
Repositories (Data Access)
   ↓
EF Core DbContext
```

## Prerequisites

- Docker & Docker Compose
- (Or: .NET 10 SDK, PostgreSQL 17, Apache Kafka for local development)

## Quick Start

```bash
docker compose up --build
```

The API will be available at `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`
- Kafka is accessible at `localhost:29092`

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | `Host=postgres;...` | PostgreSQL connection string |
| `Kafka__BootstrapServers` | `kafka:9092` | Kafka broker address |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |

## API Endpoints

### Credit Cards

- `GET /api/credit-cards` - List all credit cards (paginated)
- `GET /api/credit-cards/{id}` - Get a credit card
- `POST /api/credit-cards` - Create a credit card
- `PUT /api/credit-cards/{id}` - Update a credit card
- `DELETE /api/credit-cards/{id}` - Delete a credit card
- `GET /api/credit-cards/{id}/transactions` - Get transactions for a card

### Transactions

- `GET /api/transactions` - List all transactions (paginated)
- `GET /api/transactions/{id}` - Get a transaction
- `POST /api/transactions` - Create a transaction (publishes to Kafka)
- `PUT /api/transactions/{id}` - Update a transaction
- `DELETE /api/transactions/{id}` - Delete a transaction

### Health

- `GET /health` - Health check endpoint

## Data Models

### CreditCard
```json
{
  "id": 1,
  "cardholderName": "John Doe",
  "cardNumber": "****1234",
  "brand": "VISA",
  "creditLimit": 5000.00,
  "createdAt": "2026-01-01T12:00:00Z"
}
```

### Transaction
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

## Security

- Card numbers are masked in responses (last 4 digits only)
- No sensitive data is logged
- All input is validated
- Uses HTTPS-ready configuration (HTTP in container for development)

## Testing

```bash
dotnet test tests/CreditCardApi.Tests/CreditCardApi.Tests.csproj
```

## Development

### Build
```bash
dotnet build
```

### Run locally
```bash
# Requires PostgreSQL and Kafka running
dotnet run --project src/CreditCardApi
```

### Create migrations
```bash
dotnet ef migrations add <MigrationName> -p src/CreditCardApi -o Data/Migrations
```

## Technology Stack

- **.NET 10** - Runtime and framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL
- **Npgsql** - PostgreSQL provider
- **Confluent.Kafka** - Kafka client
- **Serilog** - Structured logging
- **Swashbuckle** - Swagger/OpenAPI documentation
- **xUnit** - Testing framework
- **Testcontainers** - Integration testing

## License

MIT
