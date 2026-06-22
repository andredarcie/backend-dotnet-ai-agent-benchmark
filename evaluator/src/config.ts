import type { Category } from './types';

export const config = {
  api: {
    baseUrl: process.env.BENCH_BASE_URL ?? 'http://localhost:8080',
    healthPath: '/health',
    bootTimeoutMs: Number(process.env.BENCH_BOOT_MS ?? 180_000),
    requestTimeoutMs: Number(process.env.BENCH_REQ_MS ?? 10_000),
  },
  kafka: {
    brokers: (process.env.BENCH_KAFKA ?? 'localhost:29092').split(','),
    topic: process.env.BENCH_KAFKA_TOPIC ?? 'transactions',
    waitMs: Number(process.env.BENCH_KAFKA_WAIT_MS ?? 25_000),
  },
  stress: {
    concurrency: Number(process.env.BENCH_STRESS_CONCURRENCY ?? 50),
    durationMs: Number(process.env.BENCH_STRESS_MS ?? 15_000),
    maxErrorRate: Number(process.env.BENCH_STRESS_MAX_ERR ?? 0.01), // < 1%
    minRps: Number(process.env.BENCH_STRESS_MIN_RPS ?? 50), // sustained throughput floor
    maxP95Ms: Number(process.env.BENCH_STRESS_MAX_P95 ?? 1_000),
  },
  docker: {
    upTimeoutMs: Number(process.env.BENCH_UP_MS ?? 360_000),
    downTimeoutMs: 120_000,
  },
};

// Single source of truth for scoring weights — mirrors REQUIREMENTS.md.
export const WEIGHTS = {
  static: {
    composePresent: 2,
    composePostgres: 2,
    composeBuildsApi: 2,
    dockerfile: 2,
    composeKafka: 2,
    twoControllers: 3,
    twoEntities: 3,
    usesOrm: 3,
    postgresProvider: 2,
    relationship: 2,
    kafkaClientPublish: 2,
    targetNet10: 5, // wrong .NET version is a contract violation → penalized here
  },
  architecture: {
    repositoryLayer: 2,
    baseRepository: 2,
    useCaseLayer: 3,
    controllersUseUseCases: 2,
    repositoriesOwnEf: 1,
  },
  build: {
    composeUp: 8,
    healthy: 7,
  },
  functional: 25, // split evenly across the assertions actually run
  kafka: {
    brokerReachable: 5,
    eventPublished: 10,
    healthcheck: 3, // compose has a Kafka healthcheck
    durability: 2, // producer configured durable (Acks.All / idempotence + retries)
  },
  stress: {
    errorRate: 6,
    throughput: 2,
    p95: 2,
  },
  quality: {
    noContainerName: 2, // no hardcoded container_name (allows isolation)
    kraftNoZookeeper: 2, // modern Kafka (KRaft, no Zookeeper)
    kafkaRecentVersion: 1, // up-to-date Kafka image
    cancellation: 3, // CancellationToken propagated controller→repo
    responseDtos: 2, // returns DTOs, doesn't leak entities
    structuredErrors: 2, // ProblemDetails / IExceptionHandler / Result pattern
    migrations: 2, // EF migrations rather than EnsureCreated
    nonRoot: 1, // container runs as non-root
    publishResilient: 3, // a Kafka publish failure doesn't 500 the request (catch, no rethrow)
    loc: 2, // concise codebase (graded by lines of code)
  },
};

export const CATEGORY_TITLES: Record<Category, string> = {
  static: '1. Static requirements',
  architecture: '2. Architecture (layering)',
  build: '3. Build & boot',
  functional: '4. Functional behavior',
  kafka: '5. Kafka integration',
  stress: '6. Stress / load',
  quality: '7. Best practices (quality)',
};
