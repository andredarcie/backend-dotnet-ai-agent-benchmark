import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { runArchitectureChecks } from '../src/checks/architecture';
import { runKafkaStaticChecks, runQualityChecks } from '../src/checks/quality';
import { runStaticChecks } from '../src/checks/static';
import { distinctCaptures, stripComments, type SourceFile } from '../src/files';
import { buildCategories, finalizeReport } from '../src/report';
import type { RoslynResult } from '../src/roslyn';
import { check, round1 } from '../src/util';

const file = (name: string, content: string): SourceFile => ({ rel: name, abs: '/x/' + name, name, content });

// A "good" compose: no container_name, no Zookeeper, recent Kafka, with a Kafka healthcheck.
const COMPOSE = file(
  'docker-compose.yml',
  `services:
  db:
    image: postgres:16-alpine
  kafka:
    image: apache/kafka:3.9.0
    healthcheck:
      test: ["CMD-SHELL", "kafka-topics.sh --bootstrap-server localhost:9092 --list"]
  api:
    build: .
`,
);
const DOCKERFILE = file('Dockerfile', "FROM mcr.microsoft.com/dotnet/aspnet:10.0\nUSER $APP_UID");

const FULL_ROSLYN: RoslynResult = {
  ok: true,
  targetFrameworks: ['net10.0'],
  controllers: ['CreditCardsController', 'TransactionsController'],
  dbContexts: ['AppDbContext'],
  entities: ['CreditCard', 'Transaction'],
  usesEfNamespace: true,
  useNpgsql: true,
  relationship: true,
  kafkaClient: true,
  kafkaProduce: true,
  hasRepositoryInterface: true,
  hasRepositoryImpl: true,
  baseRepository: true,
  useCases: ['CreateCreditCardUseCase', 'GetCreditCardByIdUseCase'],
  oneFilePerUseCase: true,
  controllerUsesUseCase: true,
  controllerTouchesDbContext: false,
  useCaseTouchesDbContext: false,
  repoUsesEf: true,
  minimalApiResourceEndpoints: 0,
  controllersUseCancellation: true,
  reposUseCancellation: true,
  responseDtoTypes: ['CreditCardResponse', 'TransactionResponse'],
  controllersUseDtos: true,
  useCasesReturnDtos: true,
  usesExceptionHandler: true,
  usesProblemDetails: true,
  usesResultPattern: false,
  databaseMigrate: true,
  kafkaDurable: true,
  kafkaPublishResilient: true,
  kafkaPublishOutbox: false,
};

const sum = (xs: { earned: number }[]) => round1(xs.reduce((s, x) => s + x.earned, 0));
const maxSum = (xs: { weight: number }[]) => round1(xs.reduce((s, x) => s + x.weight, 0));
const passed = (xs: { id: string; passed: boolean }[], id: string) => xs.find((r) => r.id === id)!.passed;

describe('util.check', () => {
  it('earns full weight when passed, zero when failed', () => {
    assert.equal(check('a', 'static', 'x', 3, true).earned, 3);
    assert.equal(check('a', 'static', 'x', 3, false).earned, 0);
  });
});

describe('files helpers', () => {
  it('stripComments removes // and /* */ but keeps code', () => {
    const s = stripComments('var x = 1; // DbContext here\n/* DbSet<T> */ doWork();');
    assert.ok(!s.includes('DbContext') && !s.includes('DbSet<T>') && s.includes('doWork()'));
  });
  it('distinctCaptures extracts unique group 1', () => {
    const f = file('a.cs', 'class CreditCardsController{} class TransactionsController{} class CreditCardsController{}');
    assert.deepEqual([...distinctCaptures([f], /class\s+(\w+Controller)\b/g)].sort(), ['CreditCardsController', 'TransactionsController']);
  });
});

describe('static checks (Roslyn mode)', () => {
  it('a complete .NET 10 submission earns the full 28', () => {
    const res = runStaticChecks([COMPOSE, DOCKERFILE], FULL_ROSLYN);
    assert.equal(maxSum(res), 28);
    assert.equal(sum(res), 28, JSON.stringify(res.filter((r) => !r.passed).map((r) => r.id)));
  });
  it('wrong .NET version is penalized (net8 fails the net10 check, −3)', () => {
    const res = runStaticChecks([COMPOSE, DOCKERFILE], { ...FULL_ROSLYN, targetFrameworks: ['net8.0'] });
    assert.equal(passed(res, 'static.net10'), false);
    assert.equal(sum(res), 25);
  });
  it('primary-constructor DbContext passes the ORM check', () => {
    assert.equal(passed(runStaticChecks([COMPOSE, DOCKERFILE], FULL_ROSLYN), 'static.orm'), true);
  });
});

describe('static checks (regex fallback, roslyn=null)', () => {
  it('classic EF-Core .NET 10 style earns the full 28', () => {
    const cs = [
      file('App.csproj', '<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>'),
      file('AppDbContext.cs', `using Microsoft.EntityFrameworkCore;
         public class AppDbContext : DbContext {
           public DbSet<CreditCard> CreditCards { get; }
           public DbSet<Transaction> Transactions { get; }
         }
         public class CreditCard {}
         public class Transaction { public int CreditCardId { get; set; } }`),
      file('CreditCardsController.cs', 'public class CreditCardsController : ControllerBase {}'),
      file('TransactionsController.cs', 'public class TransactionsController : ControllerBase {}'),
      file('Program.cs', 'using Confluent.Kafka; options.UseNpgsql(cs); await producer.ProduceAsync(t, m);'),
    ];
    const res = runStaticChecks([COMPOSE, DOCKERFILE, ...cs], null);
    assert.equal(sum(res), 28, JSON.stringify(res.filter((r) => !r.passed).map((r) => r.id)));
  });
});

describe('architecture checks (Roslyn mode)', () => {
  it('clean layering earns the full 10', () => {
    const res = runArchitectureChecks([], FULL_ROSLYN);
    assert.equal(maxSum(res), 10);
    assert.equal(sum(res), 10);
  });
  it('controller touching DbContext fails the controller→use-case check', () => {
    assert.equal(passed(runArchitectureChecks([], { ...FULL_ROSLYN, controllerTouchesDbContext: true }), 'arch.controllerUseCase'), false);
  });
});

describe('quality checks', () => {
  it('a clean, concise submission earns the full 18', () => {
    const res = runQualityChecks([COMPOSE, DOCKERFILE], FULL_ROSLYN);
    assert.equal(maxSum(res), 18);
    assert.equal(sum(res), 18, JSON.stringify(res.filter((r) => !r.passed).map((r) => r.id)));
  });
  it('resilient publish passes only when the catch does not rethrow', () => {
    assert.equal(passed(runQualityChecks([COMPOSE, DOCKERFILE], FULL_ROSLYN), 'quality.kafkaResilient'), true);
    assert.equal(passed(runQualityChecks([COMPOSE, DOCKERFILE], { ...FULL_ROSLYN, kafkaPublishResilient: false }), 'quality.kafkaResilient'), false);
  });
  it('hardcoded container_name + Zookeeper + old Kafka are penalized', () => {
    const badCompose = file('docker-compose.yml', `services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
  kafka:
    image: confluentinc/cp-kafka:7.5.0
    container_name: kafka_broker
`);
    const res = runQualityChecks([badCompose, DOCKERFILE], FULL_ROSLYN);
    assert.equal(passed(res, 'quality.noContainerName'), false);
    assert.equal(passed(res, 'quality.kraft'), false);
    assert.equal(passed(res, 'quality.kafkaVersion'), false);
  });
  it('no CancellationToken and entity-leaking responses are penalized', () => {
    const res = runQualityChecks([COMPOSE, DOCKERFILE], { ...FULL_ROSLYN, controllersUseCancellation: false, responseDtoTypes: [], controllersUseDtos: false, useCasesReturnDtos: false });
    assert.equal(passed(res, 'quality.cancellation'), false);
    assert.equal(passed(res, 'quality.dtos'), false);
  });
  it('DTOs returned by use cases count even if the controller never names the type', () => {
    const res = runQualityChecks([COMPOSE, DOCKERFILE], { ...FULL_ROSLYN, controllersUseDtos: false, useCasesReturnDtos: true });
    assert.equal(passed(res, 'quality.dtos'), true);
  });
  it('KRaft is recognized even when "ZooKeeper" appears only in a comment', () => {
    const kraftCompose = file('docker-compose.yml', `services:
  kafka:
    image: apache/kafka:3.9.0
    environment:
      # --- KRaft mode (no ZooKeeper) ---
      KAFKA_PROCESS_ROLES: broker,controller
`);
    assert.equal(passed(runQualityChecks([kraftCompose, DOCKERFILE], FULL_ROSLYN), 'quality.kraft'), true);
  });
});

describe('kafka static checks', () => {
  it('rewards a Kafka healthcheck and a durable producer', () => {
    const res = runKafkaStaticChecks([COMPOSE], FULL_ROSLYN);
    assert.equal(passed(res, 'kafka.healthcheck'), true);
    assert.equal(passed(res, 'kafka.durability'), true);
  });
  it('penalizes missing healthcheck and default acks', () => {
    const noHealth = file('docker-compose.yml', 'services:\n  kafka:\n    image: confluentinc/cp-kafka:7.5.0\n');
    const res = runKafkaStaticChecks([noHealth], { ...FULL_ROSLYN, kafkaDurable: false });
    assert.equal(passed(res, 'kafka.healthcheck'), false);
    assert.equal(passed(res, 'kafka.durability'), false);
  });
});

describe('report aggregation', () => {
  it('finalizeReport sums categories and computes percent', () => {
    const checks = [
      check('a', 'static', 'x', 30, true),
      check('b', 'architecture', 'y', 10, true),
      check('c', 'build', 'z', 10, false),
    ];
    const r = finalizeReport('demo', '/x', false, checks, []);
    assert.equal(r.totalMax, 50);
    assert.equal(r.totalEarned, 40);
    assert.equal(r.percent, 80);
  });
  it('buildCategories groups and keeps only non-empty categories', () => {
    const cats = buildCategories([check('a', 'static', 'x', 5, true), check('b', 'quality', 'y', 5, false)]);
    assert.deepEqual(cats.map((c) => c.category).sort(), ['quality', 'static']);
  });
});
