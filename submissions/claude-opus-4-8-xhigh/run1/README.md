# Credit Card REST API

A .NET 10 + ASP.NET Core (Controllers) REST API for credit cards and transactions, backed by
PostgreSQL (EF Core) and publishing a Kafka event on every created transaction.

## Run

From this folder:

```bash
docker compose up --build
```

This starts three services:

| Service  | Address (host)        | Notes                                   |
|----------|-----------------------|-----------------------------------------|
| API      | http://localhost:8080 | ASP.NET Core, listens on 8080           |
| Postgres | localhost:5432        | db `creditcards`, user/pass `postgres`  |
| Kafka    | localhost:29092       | external listener; `kafka:9092` inside  |

The database schema is created automatically on startup (`EnsureCreated`, with retry while
Postgres warms up). The `transactions` Kafka topic is created on startup (and broker
auto-creation is also enabled as a fallback). No manual setup steps are required.

## Architecture

Strict layering: **Controller → Use Case → Repository → EF Core / DbContext**.

```
Controllers/        Thin controllers — parse request, call a use case, map Result -> HTTP.
UseCases/           Application layer: one use-case class per operation (its own file).
                    The Kafka publish for a created transaction lives here.
Repositories/       Only layer that touches the DbContext. IRepository<T> + RepositoryBase<T>
                    generic base, with ICreditCardRepository / ITransactionRepository
                    concrete EF Core implementations.
Data/               AppDbContext (EF Core).
Models/             CreditCard and Transaction entities (1:N).
DTOs/               Request/response contracts (camelCase JSON).
Messaging/          Confluent.Kafka producer + startup topic initializer.
Application/        Result type + entity->DTO mapper shared by the use cases.
```

## Endpoints

| Method | Route                                   | Success | Errors                              |
|--------|-----------------------------------------|---------|-------------------------------------|
| GET    | `/health`                               | 200     | —                                   |
| GET    | `/api/credit-cards`                     | 200     | —                                   |
| GET    | `/api/credit-cards/{id}`                | 200     | 404                                 |
| POST   | `/api/credit-cards`                     | 201     | 400                                 |
| PUT    | `/api/credit-cards/{id}`                | 200     | 404 / 400                           |
| DELETE | `/api/credit-cards/{id}`                | 204     | 404                                 |
| GET    | `/api/credit-cards/{id}/transactions`   | 200     | 404                                 |
| GET    | `/api/transactions`                     | 200     | —                                   |
| GET    | `/api/transactions/{id}`                | 200     | 404                                 |
| POST   | `/api/transactions`                     | 201     | 400                                 |
| PUT    | `/api/transactions/{id}`                | 200     | 404 / 400                           |
| DELETE | `/api/transactions/{id}`                | 204     | 404                                 |

### Examples

```bash
# Create a credit card
curl -i -X POST http://localhost:8080/api/credit-cards \
  -H 'Content-Type: application/json' \
  -d '{"cardholderName":"Ada Lovelace","cardNumber":"4111111111111111","brand":"VISA","creditLimit":5000}'

# Create a transaction (publishes to the "transactions" Kafka topic)
curl -i -X POST http://localhost:8080/api/transactions \
  -H 'Content-Type: application/json' \
  -d '{"creditCardId":1,"amount":199.90,"merchant":"Amazon","category":"shopping"}'
```

## Kafka

On `POST /api/transactions` (201), the use case publishes to topic **`transactions`**:

- **key:** transaction `id` (string)
- **value:** the created transaction as camelCase JSON

Inspect from the host using the external listener `localhost:29092`, e.g.:

```bash
docker exec -it creditcard-kafka \
  kafka-console-consumer --bootstrap-server localhost:9092 --topic transactions --from-beginning
```
