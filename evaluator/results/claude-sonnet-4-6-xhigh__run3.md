# Benchmark report - `claude-sonnet-4-6-xhigh/run3`

**Score: 47 / 126 (37.3%)** - did not boot ❌
**Analysis engine:** `roslyn`

| Category | Score |
|----------|------:|
| 1. Static requirements | 25 / 28 |
| 2. Architecture (layering) | 10 / 10 |
| 3. Build & boot | 0 / 15 |
| 4. Functional behavior | 0 / 25 |
| 5. Kafka integration | 3 / 20 |
| 6. Stress / load | 0 / 10 |
| 7. Best practices (quality) | 9 / 18 |
| **Total** | **47 / 126** |

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
| ✅ | Uses EF Core (namespace + DbContext subclass) [roslyn] | 3/3 | efNamespace=true, dbContexts=[AppDbContext] |
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

### 3. Build & boot - 0/15

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ❌ | docker compose up | 0/8 | exit=17 |
| ❌ | API becomes healthy | 0/7 | not healthy within 180000ms |

### 4. Functional behavior - 0/25

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ❌ | Functional tests | 0/25 | boot failed |

### 5. Kafka integration - 3/20

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | Kafka service has a healthcheck | 3/3 | kafka healthcheck found |
| ❌ | Durable producer (Acks.All / idempotence) | 0/2 | default acks |
| ❌ | Broker reachable on host | 0/5 | boot failed |
| ❌ | Transaction create publishes to topic | 0/8 | boot failed |
| ❌ | Event message key = transaction id | 0/2 | boot failed |

### 6. Stress / load - 0/10

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ❌ | Error rate | 0/6 | boot failed |
| ❌ | Throughput | 0/2 | boot failed |
| ❌ | p95 latency | 0/2 | boot failed |

### 7. Best practices (quality) - 9/18

| | Check | Pts | Detail |
|--|-------|----:|--------|
| ✅ | No hardcoded container_name (isolatable) | 2/2 | ok |
| ✅ | Kafka in KRaft mode (no Zookeeper) | 2/2 | no Zookeeper |
| ✅ | Up-to-date Kafka image | 1/1 | confluentinc/cp-kafka:7.6.0 (recent) |
| ❌ | CancellationToken propagated (controller→repo) [roslyn] | 0/3 | controllers=false, repos=false |
| ✅ | Uses response DTOs (no entity leakage) [roslyn] | 2/2 | dtoTypes=4, controllersUse=true, useCasesReturn=false |
| ❌ | Structured errors (ProblemDetails / IExceptionHandler / Result) [roslyn] | 0/2 | exHandler=false, problemDetails=false, result=false |
| ✅ | Production-grade schema management (EF migrations, bonus over EnsureCreated) | 2/2 | Migrations/ folder present |
| ❌ | Container runs as non-root (USER) | 0/1 |  |
| ❌ | Publish failure handled gracefully (catch-and-log or outbox) [roslyn] | 0/3 | publish failure propagates (no catch, or catch rethrows) |

### Notes

- compose up failed after 2 attempt(s): time="2026-06-22T21:52:09-03:00" level=warning msg="C:\\repos\\backend-dotnet-ai-agent-benchmark\\submissions\\claude-sonnet-4-6-xhigh\\run3\\docker-compose.yml: the attribute `version` is obsolete, it will be ignored, please remove it to avoid potential confusion" |  Service api  Building | failed to solve: process "/bin/sh -c dotnet restore CreditCardApi/CreditCardApi.csproj" did not complete successfully: exit code: 1 | 
