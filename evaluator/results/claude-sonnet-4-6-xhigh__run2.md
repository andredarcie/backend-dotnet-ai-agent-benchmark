# Benchmark report - `claude-sonnet-4-6-xhigh/run2`

**Score: 58.2 / 126 (46.2%)** - booted ✅
**Analysis engine:** `roslyn`
**Patch penalty:** -10 (build failed (NU1605): csproj pinned Microsoft.EntityFrameworkCore 9.0.0 but the Npgsql 9.0.4 provider needs >= 9.0.1. Bumped EF Core to 9.0.4 so it builds; .NET source untouched.)

**Runtime integrity (strict-db):** FAILED ❌ - Postgres(creditcarddb) holds only 1 base tables (expected ≥ 2) → the API likely did not use Postgres

| Category | Score |
|----------|------:|
| 1. Static requirements | 25 / 28 |
| 2. Architecture (layering) | 10 / 10 |
| 3. Build & boot | 15 / 15 |
| 4. Functional behavior | 2.2 / 25 |
| 5. Kafka integration | 3 / 20 |
| 6. Stress / load | 4 / 10 |
| 7. Best practices (quality) | 9 / 18 |
| **Total** | **58.2 / 126** |

### Stress metrics

- Requests: **12340** (822.7 req/s), errors: **12340** (100.00%)
- Latency: p50 **54ms**, p95 **111ms**, p99 **167ms**

### 1. Static requirements - 25/28

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | docker-compose file present | 2/2 | docker-compose.yml |
| ✅ | Compose uses a Postgres service | 2/2 |  |
| ✅ | Compose builds the API image (build:) | 2/2 |  |
| ✅ | Dockerfile present | 2/2 | Dockerfile |
| ✅ | Compose has a Kafka service | 2/2 |  |
| ✅ | At least 2 controllers [roslyn] | 3/3 | found: CreditCardsController, HealthController, TransactionsController |
| ✅ | At least 2 entities (DbSet<> of real classes) [roslyn] | 3/3 | entities: CreditCard, Transaction |
| ✅ | Uses EF Core (namespace + DbContext subclass) [roslyn] | 3/3 | efNamespace=true, dbContexts=[CreditCardDbContext] |
| ✅ | Wires the Npgsql/Postgres provider (UseNpgsql) [roslyn] | 2/2 | UseNpgsql(...) found |
| ✅ | Models a 1:N relationship (FK) [roslyn] | 2/2 |  |
| ✅ | Kafka client + produce call [roslyn] | 2/2 | client=true, produce=true |
| ❌ | Targets .NET 10 [roslyn] | 0/3 | targetFrameworks: net9.0 |

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

### 4. Functional behavior - 2.2/25

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | GET /health → 200 | 1.1/1.1 | status=200 |
| ❌ | POST /api/credit-cards → 201 + numeric id | 0/1.1 | status=500, id=undefined |
| ❌ | POST /api/credit-cards sets Location header | 0/1.1 | location=(none) |
| ❌ | Created credit card echoes all fields (camelCase) | 0/1.1 | fields: (none) |
| ❌ | GET /api/credit-cards/{id} → 200 | 0/1.1 | no card id |
| ❌ | GET /api/credit-cards → 200 array | 0/1.1 | status=500, isArray=false |
| ✅ | POST /api/credit-cards empty name → 400 | 1.1/1.1 | status=400 |
| ❌ | GET missing credit card → 404 | 0/1.1 | status=500 |
| ❌ | POST /api/transactions → 201 + id | 0/1.1 | no card id |
| ❌ | GET /api/transactions/{id} → 200 | 0/1.1 | no txn id |
| ❌ | GET /api/transactions → 200 array | 0/1.1 | status=500 |
| ❌ | POST txn amount<=0 → 400 | 0/1.1 | no card id |
| ❌ | POST /api/transactions bad creditCardId → 400 | 0/1.1 | status=500 |
| ❌ | GET card transactions → 200 | 0/1.1 | no card id |
| ❌ | GET transactions for missing card → 404 | 0/1.1 | status=500 |
| ❌ | PUT txn → 200/204 | 0/1.1 | no ids |
| ❌ | DELETE txn → 204 | 0/1.1 | no txn id |
| ❌ | GET deleted txn → 404 | 0/1.1 | no txn id |
| ❌ | PUT card → 200/204 | 0/1.1 | no card id |
| ❌ | PUT missing credit card → 404 | 0/1.1 | status=500 |
| ❌ | PUT missing txn → 404 | 0/1.1 | no card id |
| ❌ | DELETE card → 204 | 0/1.1 | no card id |
| ❌ | GET deleted card → 404 | 0/1.1 | no card id |

### 5. Kafka integration - 3/20

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | Kafka service has a healthcheck | 3/3 | kafka healthcheck found |
| ❌ | Durable producer (Acks.All / idempotence) | 0/2 | default acks |
| ❌ | Broker reachable on host (localhost:29092) | 0/5 | connect failed: KafkaJSProtocolError: This server does not host this topic-partition |
| ❌ | Transaction create publishes to topic | 0/8 | skipped (broker unreachable) |
| ❌ | Event message key = transaction id | 0/2 | skipped (broker unreachable) |

### 6. Stress / load - 4/10

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ❌ | Error rate < 1% | 0/6 | errorRate=100.00% (12340/12340) |
| ✅ | Sustained throughput ≥ 50 req/s | 2/2 | 822.7 req/s, 12340 total |
| ✅ | p95 latency < 1000ms | 2/2 | p95=111ms |

### 7. Best practices (quality) - 9/18

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | No hardcoded container_name (isolatable) | 2/2 | ok |
| ✅ | Kafka in KRaft mode (no Zookeeper) | 2/2 | no Zookeeper |
| ✅ | Up-to-date Kafka image | 1/1 | confluentinc/cp-kafka:7.6.0 (recent) |
| ❌ | CancellationToken propagated (controller→repo) [roslyn] | 0/3 | controllers=false, repos=false |
| ✅ | Uses response DTOs (no entity leakage) [roslyn] | 2/2 | dtoTypes=2, controllersUse=true, useCasesReturn=false |
| ❌ | Structured errors (ProblemDetails / IExceptionHandler / Result) [roslyn] | 0/2 | exHandler=false, problemDetails=false, result=false |
| ✅ | Production-grade schema management (EF migrations, bonus over EnsureCreated) | 2/2 | Migrations/ folder present |
| ❌ | Container runs as non-root (USER) | 0/1 |  |
| ❌ | Publish failure handled gracefully (catch-and-log or outbox) [roslyn] | 0/3 | publish failure propagates (no catch, or catch rethrows) |

### Notes

- Patched to run (penalty -10): build failed (NU1605): csproj pinned Microsoft.EntityFrameworkCore 9.0.0 but the Npgsql 9.0.4 provider needs >= 9.0.1. Bumped EF Core to 9.0.4 so it builds; .NET source untouched.
- Stress did not fully pass (conservative median across 2 attempt(s)) - may indicate a loaded host or a real tail-latency issue.
