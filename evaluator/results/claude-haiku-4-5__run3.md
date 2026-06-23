# Benchmark report - `claude-haiku-4-5/run3`

**Score: 97 / 126 (77%)** - booted ✅
**Analysis engine:** `roslyn`

**Runtime integrity (strict-db):** VERIFIED ✅ - Postgres(creditcard) holds 2 base tables → schema was persisted to a real Postgres

| Category | Score |
|----------|------:|
| 1. Static requirements | 28 / 28 |
| 2. Architecture (layering) | 10 / 10 |
| 3. Build & boot | 15 / 15 |
| 4. Functional behavior | 25 / 25 |
| 5. Kafka integration | 5 / 20 |
| 6. Stress / load | 10 / 10 |
| 7. Best practices (quality) | 4 / 18 |
| **Total** | **97 / 126** |

### Stress metrics

- Requests: **6408** (427.2 req/s), errors: **0** (0.00%)
- Latency: p50 **105ms**, p95 **220ms**, p99 **312ms**

### 1. Static requirements - 28/28

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | docker-compose file present | 2/2 | docker-compose.yml |
| ✅ | Compose uses a Postgres service | 2/2 |  |
| ✅ | Compose builds the API image (build:) | 2/2 |  |
| ✅ | Dockerfile present | 2/2 | Dockerfile |
| ✅ | Compose has a Kafka service | 2/2 |  |
| ✅ | At least 2 controllers [roslyn] | 3/3 | found: CreditCardsController, HealthController, TransactionsController |
| ✅ | At least 2 entities (DbSet<> of real classes) [roslyn] | 3/3 | entities: CreditCard, Transaction |
| ✅ | Uses EF Core (namespace + DbContext subclass) [roslyn] | 3/3 | efNamespace=true, dbContexts=[AppDbContext] |
| ✅ | Wires the Npgsql/Postgres provider (UseNpgsql) [roslyn] | 2/2 | UseNpgsql(...) found |
| ✅ | Models a 1:N relationship (FK) [roslyn] | 2/2 |  |
| ✅ | Kafka client + produce call [roslyn] | 2/2 | client=true, produce=true |
| ✅ | Targets .NET 10 [roslyn] | 3/3 | targetFrameworks: net10.0 |

### 2. Architecture (layering) - 10/10

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | Repository layer (interface + implementation) [roslyn] | 2/2 | interface=true, impl=true |
| ✅ | Generic base repository class [roslyn] | 2/2 |  |
| ✅ | Use-case layer (*UseCase classes) [roslyn] | 3/3 | 11 use case(s) |
| ✅ | Controllers call use cases (not DbContext) [roslyn] | 2/2 | usesUseCase=true, touchesDb=false |
| ✅ | Repositories own EF Core / DbContext access [roslyn] | 1/1 |  |

### 3. Build & boot - 15/15

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | docker compose up | 8/8 | compose started (within 2 attempts) |
| ✅ | API becomes healthy | 7/7 | health OK |

### 4. Functional behavior - 25/25

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | GET /health → 200 | 1/1 | status=200 |
| ✅ | POST /api/credit-cards → 201 + numeric id | 1/1 | status=201, id=1 |
| ✅ | POST /api/credit-cards sets Location header | 1/1 | location=http://localhost:8080/api/credit-cards/1 |
| ✅ | Created credit card echoes all fields (camelCase) | 1/1 | fields: id,cardholderName,cardNumber,brand,creditLimit,createdAt |
| ✅ | GET /api/credit-cards/{id} → 200 + matches | 1/1 | status=200 |
| ✅ | GET /api/credit-cards → 200 array | 1/1 | status=200, isArray=true |
| ✅ | POST /api/credit-cards empty name → 400 | 1/1 | status=400 |
| ✅ | GET missing credit card → 404 | 1/1 | status=404 |
| ✅ | POST /api/transactions → 201 + numeric id | 1/1 | status=201, id=1 |
| ✅ | POST /api/transactions sets Location header | 1/1 | location=http://localhost:8080/api/transactions/1 |
| ✅ | Created transaction echoes all fields (camelCase) | 1/1 | fields: id,creditCardId,amount,merchant,category,createdAt |
| ✅ | GET /api/transactions/{id} → 200 | 1/1 | status=200 |
| ✅ | GET /api/transactions → 200 array | 1/1 | status=200 |
| ✅ | POST /api/transactions amount<=0 → 400 | 1/1 | status=400 |
| ✅ | POST /api/transactions bad creditCardId → 400 | 1/1 | status=400 |
| ✅ | GET /api/credit-cards/{id}/transactions → 200 incl. txn | 1/1 | status=200 |
| ✅ | GET transactions for missing card → 404 | 1/1 | status=404 |
| ✅ | PUT /api/transactions/{id} → 200/204 | 1/1 | status=200 |
| ✅ | DELETE /api/transactions/{id} → 204 | 1/1 | status=204 |
| ✅ | GET deleted transaction → 404 | 1/1 | status=404 |
| ✅ | PUT /api/credit-cards/{id} → 200/204 + persisted | 1/1 | status=200 |
| ✅ | PUT missing credit card → 404 | 1/1 | status=404 |
| ✅ | PUT missing transaction → 404 | 1/1 | status=404 |
| ✅ | DELETE /api/credit-cards/{id} → 204 | 1/1 | status=204 |
| ✅ | GET deleted credit card → 404 | 1/1 | status=404 |

### 5. Kafka integration - 5/20

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ❌ | Kafka service has a healthcheck | 0/3 | no kafka healthcheck |
| ❌ | Durable producer (Acks.All / idempotence) | 0/2 | default acks |
| ✅ | Broker reachable on host (localhost:29092) | 5/5 | subscribed to "transactions" |
| ❌ | Transaction create publishes to topic (value + key) | 0/8 | no matching message within 25000ms (saw 2) |
| ❌ | Event message key = transaction id | 0/2 | no event received |

### 6. Stress / load - 10/10

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | Error rate < 1% | 6/6 | errorRate=0.00% (0/6408) |
| ✅ | Sustained throughput ≥ 50 req/s | 2/2 | 427.2 req/s, 6408 total |
| ✅ | p95 latency < 1000ms | 2/2 | p95=220ms |

### 7. Best practices (quality) - 4/18

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ❌ | No hardcoded container_name (isolatable) | 0/2 | container_name is hardcoded |
| ❌ | Kafka in KRaft mode (no Zookeeper) | 0/2 | runs a Zookeeper service |
| ❌ | Up-to-date Kafka image | 0/1 | confluentinc/cp-kafka:7.5.0 (outdated) |
| ❌ | CancellationToken propagated (controller→repo) [roslyn] | 0/3 | controllers=false, repos=false |
| ✅ | Uses response DTOs (no entity leakage) [roslyn] | 2/2 | dtoTypes=2, controllersUse=false, useCasesReturn=true |
| ❌ | Structured errors (ProblemDetails / IExceptionHandler / Result) [roslyn] | 0/2 | exHandler=false, problemDetails=false, result=false |
| ✅ | Production-grade schema management (EF migrations, bonus over EnsureCreated) | 2/2 | Migrations/ folder present |
| ❌ | Container runs as non-root (USER) | 0/1 |  |
| ❌ | Publish failure handled gracefully (catch-and-log or outbox) [roslyn] | 0/3 | publish failure propagates (no catch, or catch rethrows) |
