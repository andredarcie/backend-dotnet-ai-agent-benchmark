# Task: Credit Card REST API

You are a backend development agent. Build a complete, working **REST API** that follows
the contract below **exactly**. Your submission will be graded automatically: it will be
started via Docker, hit on every endpoint, and its source code will be analyzed. Follow the
contract to the letter (routes, status codes, and JSON shape) - any divergence fails the tests.

> Everything must live in **a single folder** (your project root).
> Everything must start with **one command** from that folder: `docker compose up --build`.

---

## 1. Required stack

- **Language/Framework:** .NET 10 + ASP.NET Core (Web API using **Controllers**, not Minimal API).
- **ORM:** Entity Framework Core.
- **Database:** PostgreSQL (Npgsql provider), running as a service in `docker-compose`.
- **Messaging:** Apache Kafka, running as a service in `docker-compose`. The API publishes a
  message to Kafka every time a transaction is created (see section 4).
- **Orchestration:** a `docker-compose.yml` at the project root that starts **API + Postgres + Kafka**.
- The database schema must be created **automatically** when the API starts, with no manual step.
  `EnsureCreated()` is acceptable to get a passing boot, but production-grade **EF Core migrations**
  are preferred and scored higher under best-practices.

### Required architecture (layering)

Use a **layered architecture** with this exact call chain:

```
Controller  →  Use Case  →  Repository  →  EF Core / DbContext
```

- **Controllers** are thin: they only parse the request, call a **use case**, and map the
  result to an HTTP response. A controller **must not** touch the `DbContext` or run queries
  directly.
- **Use cases** (application layer) hold the business logic and validation. Use **one use
  case class per operation, each in its own file** (e.g. `CreateTransactionUseCase`,
  `GetCreditCardByIdUseCase`). A use case talks to **repositories**, never to `DbContext` directly.
- **Repositories** are the only place that uses EF Core / the `DbContext`. Expose an
  interface per aggregate (e.g. `ICreditCardRepository`, `ITransactionRepository`) with a
  concrete EF Core implementation. Provide a **generic base repository class** (e.g.
  `RepositoryBase<T>`) that implements the common CRUD operations; concrete repositories
  inherit from it and add entity-specific queries.
- The Kafka publish for a created transaction happens in the **use case** (the application
  layer orchestrates persistence + event), not in the controller.

## 2. Domain model (2 related entities)

A **1:N** relationship - one `CreditCard` has many `Transaction`s; a `Transaction` belongs to one `CreditCard`.

### CreditCard
| Field            | Type      | Rules                              |
|------------------|-----------|------------------------------------|
| `id`             | int       | PK, auto-increment                 |
| `cardholderName` | string    | **required**, non-empty            |
| `cardNumber`     | string    | **required**, non-empty            |
| `brand`          | string?   | optional (e.g. `VISA`, `MASTERCARD`) |
| `creditLimit`    | decimal   | **required**, >= 0                 |
| `createdAt`      | datetime  | set by the server (UTC)            |

### Transaction
| Field          | Type      | Rules                                                |
|----------------|-----------|------------------------------------------------------|
| `id`           | int       | PK, auto-increment                                   |
| `creditCardId` | int       | **required FK** → must reference an existing CreditCard |
| `amount`       | decimal   | **required**, must be **> 0**                        |
| `merchant`     | string    | **required**, non-empty                              |
| `category`     | string?   | optional                                             |
| `createdAt`    | datetime  | set by the server (UTC)                              |

> JSON uses **camelCase** for every field (the ASP.NET Core default).

## 3. HTTP contract

The API must listen on port **8080** inside the container and expose **8080** on the host.
All bodies are JSON (`Content-Type: application/json`).

### Health
- `GET /health` → **200** with `{ "status": "healthy" }`

### Credit cards - `CreditCardsController` under `api/credit-cards`
| Method | Route                                | Success | Errors                                   |
|--------|--------------------------------------|---------|------------------------------------------|
| GET    | `/api/credit-cards`                  | 200 (array) | -                                    |
| GET    | `/api/credit-cards/{id}`             | 200     | 404 if not found                         |
| POST   | `/api/credit-cards`                  | 201 (+`Location`, body with `id`) | 400 if `cardholderName` or `cardNumber` empty |
| PUT    | `/api/credit-cards/{id}`             | 200 or 204 | 404 if not found; 400 if invalid      |
| DELETE | `/api/credit-cards/{id}`             | 204     | 404 if not found                         |
| GET    | `/api/credit-cards/{id}/transactions`| 200 (array of that card's transactions) | 404 if card not found |

### Transactions - `TransactionsController` under `api/transactions`
| Method | Route                     | Success | Errors                                                              |
|--------|---------------------------|---------|---------------------------------------------------------------------|
| GET    | `/api/transactions`       | 200 (array) | -                                                               |
| GET    | `/api/transactions/{id}`  | 200     | 404 if not found                                                    |
| POST   | `/api/transactions`       | 201 (+`Location`, body with `id`) | 400 if `merchant` empty, `amount` <= 0, or `creditCardId` does not exist |
| PUT    | `/api/transactions/{id}`  | 200 or 204 | 404 if not found; 400 if invalid or `creditCardId` does not exist |
| DELETE | `/api/transactions/{id}`  | 204     | 404 if not found                                                    |

### Response shapes (examples)

`POST /api/credit-cards` with `{ "cardholderName": "Ada Lovelace", "cardNumber": "4111111111111111", "brand": "VISA", "creditLimit": 5000 }` → **201**:
```json
{ "id": 1, "cardholderName": "Ada Lovelace", "cardNumber": "4111111111111111", "brand": "VISA", "creditLimit": 5000, "createdAt": "2026-01-01T12:00:00Z" }
```

`POST /api/transactions` with `{ "creditCardId": 1, "amount": 199.90, "merchant": "Amazon", "category": "shopping" }` → **201**:
```json
{ "id": 1, "creditCardId": 1, "amount": 199.90, "merchant": "Amazon", "category": "shopping", "createdAt": "2026-01-01T12:00:00Z" }
```

## 4. Event publishing (Kafka)

Every time a transaction is **created successfully** (`POST /api/transactions` → 201), the API
must publish a message to a Kafka topic. This lets downstream consumers react to new transactions.

- **Topic:** `transactions`
- **Message value:** the created transaction as JSON (camelCase), at minimum:
  ```json
  { "id": 1, "creditCardId": 1, "amount": 199.90, "merchant": "Amazon", "category": "shopping", "createdAt": "2026-01-01T12:00:00Z" }
  ```
- **Message key:** the transaction `id` (as a string). The key **must equal** the transaction id -
  this is checked and graded, so do not use a random key, a constant, or no key.
- The publish must happen **after** the transaction is persisted (do not publish for invalid
  requests that returned 400).
- Use a Kafka client for .NET (e.g. `Confluent.Kafka`).

### Kafka networking (so the broker can be inspected from outside Docker)

The broker must be reachable in **two** ways:

| From            | Bootstrap address  |
|-----------------|--------------------|
| Inside Docker (the API) | `kafka:9092` (internal listener) |
| The host machine        | `localhost:29092` (external listener, mapped to the host) |

Configure the broker's advertised listeners accordingly and **publish port `29092` to the host**
in `docker-compose.yml`. Enable automatic topic creation (or create the `transactions` topic on
startup) so the topic exists on first publish.

The API should read its broker address from configuration (default `kafka:9092`) - for example an
env var `Kafka__BootstrapServers`.

## 5. Non-functional requirements

- The API must withstand concurrent load (it will be stressed with dozens of simultaneous
  requests) without returning 5xx or hanging.
- Validate input properly and return the correct status codes (do not return 200 for everything).
- Organized code: separate Models, DbContext, DTOs, and Controllers.

## 6. Deliverables (at your project root)

1. `docker-compose.yml` - starts API + Postgres + Kafka with one command.
2. A `Dockerfile` for the API.
3. .NET source: project file, `Program.cs`, **2 controllers**, **2 entities**, a `DbContext`,
   a **repository layer**, a **use-case layer**, and a Kafka producer - wired as
   Controller → Use Case → Repository.
4. Everything must come up cleanly with `docker compose up --build`, the API reachable at
   `http://localhost:8080`, and Kafka reachable at `localhost:29092`.

Do not include any manual setup steps. If the compose is up, the API must work and transactions
must land on the `transactions` Kafka topic.
