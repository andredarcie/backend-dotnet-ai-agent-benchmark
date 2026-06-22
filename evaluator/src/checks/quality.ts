import { WEIGHTS } from '../config';
import { anyMatch, composeFiles, csFiles, type SourceFile } from '../files';
import type { RoslynResult } from '../roslyn';
import type { CheckResult } from '../types';
import { check } from '../util';

const Q = WEIGHTS.quality;
const K = WEIGHTS.kafka;

/** Parse the Kafka image tag from a compose file and decide whether it is reasonably recent. */
function kafkaImageRecent(composeText: string): { recent: boolean; detail: string } {
  const m = composeText.match(/image:\s*["']?([^\s"']*kafka[^\s"']*)/i);
  if (!m) return { recent: false, detail: 'no kafka image found' };
  const ref = m[1];
  const name = ref.replace(/:[^:/]*$/, '').toLowerCase();
  const tag = ref.match(/:([^:/]+)$/)?.[1] ?? '';
  const vm = tag.match(/(\d+)\.(\d+)/);
  if (!vm) return { recent: false, detail: `${ref} (no version tag)` };
  const major = parseInt(vm[1], 10);
  const minor = parseInt(vm[2], 10);
  let recent: boolean;
  if (name.includes('cp-kafka') || name.includes('confluent')) recent = major > 7 || (major === 7 && minor >= 6);
  else if (name.includes('apache/kafka')) recent = major > 3 || (major === 3 && minor >= 8);
  else if (name.includes('bitnami')) recent = major > 3 || (major === 3 && minor >= 7);
  else recent = major >= 3; // unknown family - be lenient
  return { recent, detail: `${ref} (${recent ? 'recent' : 'outdated'})` };
}

export function runQualityChecks(files: SourceFile[], roslyn: RoslynResult | null): CheckResult[] {
  const results: CheckResult[] = [];
  const compose = composeFiles(files);
  const composeText = compose.map((f) => f.content).join('\n');
  const cs = csFiles(files);
  const via = roslyn ? ' [roslyn]' : ' [regex]';

  const hasContainerName = /\n\s*container_name\s*:/.test(composeText);
  results.push(check('quality.noContainerName', 'quality', 'No hardcoded container_name (isolatable)', Q.noContainerName,
    compose.length > 0 && !hasContainerName, hasContainerName ? 'container_name is hardcoded' : 'ok'));

  // Detect an actual Zookeeper service/image - not the word in a comment (e.g. "# KRaft, no ZooKeeper").
  const hasZookeeper = /image:\s*\S*zookeeper/i.test(composeText) || /^\s+zookeeper\s*:/im.test(composeText);
  results.push(check('quality.kraft', 'quality', 'Kafka in KRaft mode (no Zookeeper)', Q.kraftNoZookeeper,
    compose.length > 0 && !hasZookeeper, hasZookeeper ? 'runs a Zookeeper service' : 'no Zookeeper'));

  const ver = kafkaImageRecent(composeText);
  results.push(check('quality.kafkaVersion', 'quality', 'Up-to-date Kafka image', Q.kafkaRecentVersion, ver.recent, ver.detail));

  if (roslyn) {
    const ok = roslyn.controllersUseCancellation && roslyn.reposUseCancellation;
    results.push(check('quality.cancellation', 'quality', 'CancellationToken propagated (controller→repo)' + via, Q.cancellation,
      ok, `controllers=${roslyn.controllersUseCancellation}, repos=${roslyn.reposUseCancellation}`));
  } else {
    const ctrl = anyMatch(cs.filter((f) => /Controller\b/.test(f.content)), /CancellationToken/);
    const repo = anyMatch(cs.filter((f) => /Repository\b/.test(f.content)), /CancellationToken/);
    results.push(check('quality.cancellation', 'quality', 'CancellationToken propagated (controller→repo)' + via, Q.cancellation, !!ctrl && !!repo));
  }

  if (roslyn) {
    // DTOs are "used" if controllers reference them OR use cases return them (controllers then
    // surface that via result.Value without naming the type).
    const ok = roslyn.responseDtoTypes.length > 0 && (roslyn.controllersUseDtos || roslyn.useCasesReturnDtos);
    results.push(check('quality.dtos', 'quality', 'Uses response DTOs (no entity leakage)' + via, Q.responseDtos, ok,
      `dtoTypes=${roslyn.responseDtoTypes.length}, controllersUse=${roslyn.controllersUseDtos}, useCasesReturn=${roslyn.useCasesReturnDtos}`));
  } else {
    const ok = anyMatch(cs, /(class|record)\s+\w*(Response|Dto)\b/);
    results.push(check('quality.dtos', 'quality', 'Uses response DTOs (no entity leakage)' + via, Q.responseDtos, !!ok));
  }

  if (roslyn) {
    const ok = roslyn.usesExceptionHandler || roslyn.usesProblemDetails || roslyn.usesResultPattern;
    results.push(check('quality.errors', 'quality', 'Structured errors (ProblemDetails / IExceptionHandler / Result)' + via,
      Q.structuredErrors, ok, `exHandler=${roslyn.usesExceptionHandler}, problemDetails=${roslyn.usesProblemDetails}, result=${roslyn.usesResultPattern}`));
  } else {
    const ok = anyMatch(cs, /IExceptionHandler|AddProblemDetails|ProblemDetails|\bResult</);
    results.push(check('quality.errors', 'quality', 'Structured errors (ProblemDetails / IExceptionHandler / Result)' + via, Q.structuredErrors, !!ok));
  }

  const hasMigrationsFolder = files.some((f) => /(^|[\\/])Migrations[\\/]/.test(f.rel) && f.name.toLowerCase().endsWith('.cs'));
  const migrate = hasMigrationsFolder || (roslyn ? roslyn.databaseMigrate : !!anyMatch(cs, /Database\.Migrate/));
  results.push(check('quality.migrations', 'quality', 'Production-grade schema management (EF migrations, bonus over EnsureCreated)', Q.migrations, migrate,
    hasMigrationsFolder ? 'Migrations/ folder present' : migrate ? 'Database.Migrate() call' : 'EnsureCreated only'));

  const dockerfile = files.find((f) => f.name.toLowerCase().startsWith('dockerfile'));
  const nonRoot = !!dockerfile && /^\s*USER\s+\S+/m.test(dockerfile.content);
  results.push(check('quality.nonRoot', 'quality', 'Container runs as non-root (USER)', Q.nonRoot, nonRoot));

  // Resilient Kafka publish: a publish failure is handled (catch-and-log) or routed through a
  // transactional outbox, so it doesn't 500 the request.
  if (roslyn) {
    const ok = roslyn.kafkaPublishResilient || roslyn.kafkaPublishOutbox;
    results.push(check('quality.kafkaResilient', 'quality', 'Publish failure handled gracefully (catch-and-log or outbox)' + via,
      Q.publishResilient, ok,
      ok ? 'publish in try/catch (no rethrow) or transactional outbox' : 'publish failure propagates (no catch, or catch rethrows)'));
  } else {
    const hasOutbox = !!anyMatch(cs, /class\s+\w*Outbox|DbSet<\s*\w*Outbox/i);
    // Produce sits in a try, a catch actually handles it (logs / retries / enqueues — not a silent
    // swallow), and no catch rethrows. A bare `catch {}` must NOT pass.
    const producesInTry = !!anyMatch(cs, /try[\s\S]{0,400}Produce/);
    const catchHandles = !!anyMatch(cs, /catch[^{]*\{[\s\S]{0,300}(Log|Console\.|Write|Retry|Enqueue|Save)/i);
    const rethrows = cs.some((f) => /catch[^{]*\{[\s\S]{0,200}throw\s*;/.test(f.content));
    const catchAndLog = producesInTry && catchHandles && !rethrows;
    const ok = catchAndLog || hasOutbox;
    results.push(check('quality.kafkaResilient', 'quality', 'Publish failure handled gracefully (catch-and-log or outbox)' + via, Q.publishResilient, ok,
      ok ? 'catch-and-log or transactional outbox' : 'publish failure propagates (no catch/log or transactional outbox)'));
  }

  return results;
}

/** Static Kafka-category checks (always run, no Docker): healthcheck + producer durability. */
export function runKafkaStaticChecks(files: SourceFile[], roslyn: RoslynResult | null): CheckResult[] {
  const results: CheckResult[] = [];
  const composeText = composeFiles(files).map((f) => f.content).join('\n');
  const cs = csFiles(files);

  const kafkaHealth =
    /(kafka-topics|kafka-broker-api-versions|kafka-ready|cub\s+kafka-ready|nc\s+-z[^\n]*9092)/i.test(composeText) ||
    /healthcheck[\s\S]{0,300}9092/i.test(composeText);
  results.push(check('kafka.healthcheck', 'kafka', 'Kafka service has a healthcheck', K.healthcheck, kafkaHealth,
    kafkaHealth ? 'kafka healthcheck found' : 'no kafka healthcheck'));

  const durable = roslyn ? roslyn.kafkaDurable : !!anyMatch(cs, /Acks\.All|EnableIdempotence/);
  results.push(check('kafka.durability', 'kafka', 'Durable producer (Acks.All / idempotence)', K.durability, durable,
    durable ? 'Acks.All / idempotence configured' : 'default acks'));

  return results;
}
