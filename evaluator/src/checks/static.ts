import { WEIGHTS } from '../config';
import { anyMatch, composeFiles, csFiles, distinctCaptures, type SourceFile } from '../files';
import type { RoslynResult } from '../roslyn';
import type { CheckResult } from '../types';
import { check } from '../util';

const W = WEIGHTS.static;

export function runStaticChecks(files: SourceFile[], roslyn: RoslynResult | null): CheckResult[] {
  const results: CheckResult[] = [];
  const compose = composeFiles(files);
  const composeText = compose.map((f) => f.content).join('\n');
  const cs = csFiles(files);
  const via = roslyn ? ' [roslyn]' : ' [regex]';

  // --- docker-compose / Dockerfile (file-based; Roslyn does not cover YAML) ---
  results.push(
    check('static.compose', 'static', 'docker-compose file present', W.composePresent, compose.length > 0,
      compose.length ? compose.map((f) => f.rel).join(', ') : 'no docker-compose.(yml|yaml) found'),
  );
  const hasPg = /image:\s*\S*postgres/i.test(composeText) || /\bpostgres\b/i.test(composeText);
  results.push(check('static.compose.postgres', 'static', 'Compose uses a Postgres service', W.composePostgres, hasPg));
  results.push(check('static.compose.build', 'static', 'Compose builds the API image (build:)', W.composeBuildsApi, /\n\s*build\s*:/.test(composeText)));
  const dockerfile = files.find((f) => f.name.toLowerCase().startsWith('dockerfile'));
  results.push(check('static.dockerfile', 'static', 'Dockerfile present', W.dockerfile, !!dockerfile, dockerfile?.rel));
  const hasKafka = /image:\s*\S*kafka/i.test(composeText) || /\n\s*kafka\s*:/i.test(composeText);
  results.push(check('static.compose.kafka', 'static', 'Compose has a Kafka service', W.composeKafka, hasKafka));

  // --- C# structure (Roslyn if available, else regex) ---

  // two controllers
  if (roslyn) {
    results.push(check('static.controllers', 'static', 'At least 2 controllers' + via, W.twoControllers,
      roslyn.controllers.length >= 2, `found: ${roslyn.controllers.join(', ') || 'none'}`));
  } else {
    const controllers = distinctCaptures(cs, /class\s+(\w+Controller)\b/g);
    results.push(check('static.controllers', 'static', 'At least 2 controllers' + via, W.twoControllers,
      controllers.size >= 2, `found: ${[...controllers].join(', ') || 'none'}`));
  }

  // two entities
  if (roslyn) {
    results.push(check('static.entities', 'static', 'At least 2 entities (DbSet<> of real classes)' + via, W.twoEntities,
      roslyn.entities.length >= 2, `entities: ${roslyn.entities.join(', ') || 'none'}`));
  } else {
    const dbSetTypes = distinctCaptures(cs, /DbSet<\s*(\w+)\s*>/g);
    const classNames = distinctCaptures(cs, /\bclass\s+(\w+)/g);
    const entities = [...dbSetTypes].filter((t) => classNames.has(t));
    results.push(check('static.entities', 'static', 'At least 2 entities (DbSet<> of real classes)' + via, W.twoEntities,
      entities.length >= 2, `entities: ${entities.join(', ') || 'none'}`));
  }

  // uses an ORM (EF Core)
  if (roslyn) {
    const ok = roslyn.usesEfNamespace && roslyn.dbContexts.length > 0;
    results.push(check('static.orm', 'static', 'Uses EF Core (namespace + DbContext subclass)' + via, W.usesOrm, ok,
      `efNamespace=${roslyn.usesEfNamespace}, dbContexts=[${roslyn.dbContexts.join(', ')}]`));
  } else {
    const efRef = anyMatch(files, /Microsoft\.EntityFrameworkCore/);
    const dbContext = anyMatch(cs, /class\s+\w+\s*(?:\([^)]*\))?\s*:[^{};]*\bDbContext\b/);
    results.push(check('static.orm', 'static', 'Uses EF Core (reference + DbContext subclass)' + via, W.usesOrm,
      !!efRef && !!dbContext, `efRef=${!!efRef}, dbContext=${!!dbContext}`));
  }

  // Postgres provider wiring (UseNpgsql)
  if (roslyn) {
    results.push(check('static.npgsql', 'static', 'Wires the Npgsql/Postgres provider (UseNpgsql)' + via, W.postgresProvider,
      roslyn.useNpgsql, roslyn.useNpgsql ? 'UseNpgsql(...) found' : 'no UseNpgsql() call'));
  } else {
    const useNpgsql = anyMatch(cs, /UseNpgsql\s*\(/);
    const npgsqlRef = anyMatch(files, /Npgsql/);
    results.push(check('static.npgsql', 'static', 'Wires the Npgsql/Postgres provider (UseNpgsql)' + via, W.postgresProvider,
      !!useNpgsql, useNpgsql ? 'UseNpgsql(...) found' : npgsqlRef ? 'Npgsql referenced but no UseNpgsql() call' : 'no Npgsql'));
  }

  // 1:N relationship
  if (roslyn) {
    results.push(check('static.relationship', 'static', 'Models a 1:N relationship (FK)' + via, W.relationship, roslyn.relationship));
  } else {
    const relationship =
      anyMatch(cs, /HasForeignKey/) || anyMatch(cs, /\[ForeignKey/) || anyMatch(cs, /public\s+(?:virtual\s+)?int\s+\w+Id\s*\{\s*get;/);
    results.push(check('static.relationship', 'static', 'Models a 1:N relationship (FK)' + via, W.relationship, !!relationship));
  }

  // Kafka client + produce
  if (roslyn) {
    const ok = roslyn.kafkaClient && roslyn.kafkaProduce;
    results.push(check('static.kafka.publish', 'static', 'Kafka client + produce call' + via, W.kafkaClientPublish, ok,
      `client=${roslyn.kafkaClient}, produce=${roslyn.kafkaProduce}`));
  } else {
    const kafkaClient = anyMatch(files, /Confluent\.Kafka/);
    const publishCall = anyMatch(cs, /ProduceAsync|\.Produce\s*\(|IProducer\s*</);
    results.push(check('static.kafka.publish', 'static', 'Kafka client + produce call' + via, W.kafkaClientPublish,
      !!kafkaClient && !!publishCall, `client=${!!kafkaClient}, produce=${!!publishCall}`));
  }

  // Targets .NET 10 (the prompt requires it — wrong version is a contract violation).
  if (roslyn) {
    const tfms = roslyn.targetFrameworks;
    const ok = tfms.length > 0 && tfms.some((t) => /net10\./.test(t));
    results.push(check('static.net10', 'static', 'Targets .NET 10' + via, W.targetNet10, ok,
      `targetFrameworks: ${tfms.join(', ') || 'unknown'}`));
  } else {
    const csproj = files.find((f) => f.name.toLowerCase().endsWith('.csproj'));
    const ok = !!csproj && /<TargetFrameworks?>[^<]*net10\./i.test(csproj.content);
    results.push(check('static.net10', 'static', 'Targets .NET 10' + via, W.targetNet10, ok));
  }

  return results;
}
