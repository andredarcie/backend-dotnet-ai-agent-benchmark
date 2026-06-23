import { existsSync, mkdirSync, readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { runArchitectureChecks } from './checks/architecture';
import { runFunctionalChecks } from './checks/functional';
import { runKafkaChecks } from './checks/kafka';
import { runKafkaStaticChecks, runQualityChecks } from './checks/quality';
import { runStaticChecks } from './checks/static';
import { runStressChecks } from './checks/stress';
import { verifyRuntimePostgres } from './checks/strictDb';
import { config, WEIGHTS } from './config';
import { cleanupStaleBenchContainers, composeDown, composeUp, dockerAvailable, findComposeFile, portHolders, projectName, type CommandResult } from './docker';
import { readSourceFiles } from './files';
import { waitForApi } from './http';
import { analyzeWithRoslyn } from './roslyn';
import { finalizeReport, writeLeaderboard, writeReports } from './report';
import type { CheckResult, SubmissionReport } from './types';
import { c, check, round1 } from './util';

// Silence two benign Node warnings: kafkajs' internal negative-timeout warning and the
// spawn({shell:true}) deprecation. Any other warning still prints.
const _emitWarning = process.emitWarning.bind(process);
(process as any).emitWarning = (warning: unknown, ...rest: unknown[]) => {
  const text = [typeof warning === 'string' ? warning : (warning as Error)?.message ?? '', JSON.stringify(rest)].join(' ');
  if (/TimeoutNegativeWarning|negative number|DEP0190|shell option true/.test(text)) return;
  return (_emitWarning as any)(warning, ...rest);
};

const srcDir = path.dirname(fileURLToPath(import.meta.url));
const evaluatorDir = path.resolve(srcDir, '..');
const repoRoot = path.resolve(evaluatorDir, '..');
const SUBMISSIONS_DIR = path.join(repoRoot, 'submissions');
const RESULTS_DIR = path.join(evaluatorDir, 'results');

interface Options {
  names: string[];
  staticOnly: boolean;
  noKafka: boolean;
  noStress: boolean;
  keepUp: boolean;
  strictDb: boolean;
  leaderboard: boolean;
  retries: number;
  allowRegexFallback: boolean;
}

function parseArgs(argv: string[]): Options {
  const o: Options = { names: [], staticOnly: false, noKafka: false, noStress: false, keepUp: false, strictDb: false, leaderboard: false, retries: 1, allowRegexFallback: false };
  for (const arg of argv) {
    if (arg.startsWith('--retries=')) {
      o.retries = Math.max(0, parseInt(arg.slice('--retries='.length), 10) || 0);
      continue;
    }
    switch (arg) {
      case '--static-only': o.staticOnly = true; break;
      case '--no-kafka': o.noKafka = true; break;
      case '--no-stress': o.noStress = true; break;
      case '--keep-up': o.keepUp = true; break;
      case '--strict-db': o.strictDb = true; break;
      case '--leaderboard': o.leaderboard = true; break;
      case '--allow-regex-fallback': o.allowRegexFallback = true; break;
      default:
        if (!arg.startsWith('--')) o.names.push(arg);
    }
  }
  return o;
}

/** Build the leaderboard from saved results/*.json (no Docker, no re-evaluation). */
function buildLeaderboardFromResults(): void {
  const jsons = existsSync(RESULTS_DIR)
    ? readdirSync(RESULTS_DIR).filter((f) => f.endsWith('.json'))
    : [];
  const reports: SubmissionReport[] = [];
  for (const f of jsons) {
    try {
      reports.push(JSON.parse(readFileSync(path.join(RESULTS_DIR, f), 'utf8')) as SubmissionReport);
    } catch {
      /* skip unreadable/invalid report */
    }
  }
  if (!reports.length) {
    log(c.red(`No result reports found in ${RESULTS_DIR}. Evaluate some submissions first.`));
    process.exit(1);
  }
  const lb = writeLeaderboard(RESULTS_DIR, reports);
  log(c.bold(`Leaderboard (${reports.length} submission(s)): ${path.relative(repoRoot, lb)}`));
  [...reports]
    .sort((a, b) => b.totalEarned - a.totalEarned)
    .forEach((r, i) => log(`  ${i + 1}. ${r.name.padEnd(24)} ${r.totalEarned}/${r.totalMax} (${r.percent}%)${r.integrity ? (r.integrity.passed ? '  strict-db ✅' : '  strict-db ❌') : ''}`));
}

// A submission is a folder with a compose file: either `submissions/<model>/` (flat/legacy) or
// `submissions/<model>/<run>/` (the model/run layout). Names use "/" - e.g. "gpt-5-5-xhigh/run1".
function listSubmissions(): string[] {
  if (!existsSync(SUBMISSIONS_DIR)) return [];
  const names: string[] = [];
  for (const top of readdirSync(SUBMISSIONS_DIR, { withFileTypes: true })) {
    if (!top.isDirectory()) continue;
    const topDir = path.join(SUBMISSIONS_DIR, top.name);
    if (findComposeFile(topDir)) {
      names.push(top.name); // flat: submissions/<model>/
      continue;
    }
    for (const sub of readdirSync(topDir, { withFileTypes: true })) {
      if (sub.isDirectory() && findComposeFile(path.join(topDir, sub.name)))
        names.push(`${top.name}/${sub.name}`); // nested: submissions/<model>/<run>/
    }
  }
  return names.sort();
}

const functionalSkipped = (reason: string): CheckResult[] => [
  check('func.skipped', 'functional', 'Functional tests', WEIGHTS.functional, false, reason),
];
const kafkaSkipped = (reason: string): CheckResult[] => [
  check('kafka.broker', 'kafka', 'Broker reachable on host', WEIGHTS.kafka.brokerReachable, false, reason),
  check('kafka.event', 'kafka', 'Transaction create publishes to topic', WEIGHTS.kafka.eventPublished, false, reason),
  check('kafka.eventKey', 'kafka', 'Event message key = transaction id', WEIGHTS.kafka.eventKey, false, reason),
];
const stressSkipped = (reason: string): CheckResult[] => [
  check('stress.errorRate', 'stress', 'Error rate', WEIGHTS.stress.errorRate, false, reason),
  check('stress.throughput', 'stress', 'Throughput', WEIGHTS.stress.throughput, false, reason),
  check('stress.p95', 'stress', 'p95 latency', WEIGHTS.stress.p95, false, reason),
];

const log = (s: string) => process.stdout.write(s + '\n');

async function evaluate(name: string, opts: Options): Promise<SubmissionReport> {
  const dir = path.join(SUBMISSIONS_DIR, name);
  const checks: CheckResult[] = [];
  const notes: string[] = [];
  let booted = false;
  let stress: SubmissionReport['stress'];

  log('');
  log(c.bold(c.cyan(`▶ Evaluating ${name}`)));

  // --- static + architecture (no Docker) ---
  const files = readSourceFiles(dir);
  log(c.gray(`  read ${files.length} source files`));
  const roslyn = await analyzeWithRoslyn(dir, (s) => log(c.gray('  ' + s)));
  if (roslyn === null && !opts.allowRegexFallback) {
    log(c.red('  ✖ Roslyn analysis unavailable - the .NET SDK is required for reproducible, precise scoring.'));
    log(c.red('    Install the .NET SDK so the Roslyn analyzer can run, or pass --allow-regex-fallback to'));
    log(c.red('    score with the (less precise) regex engine. Aborting to avoid producing misleading scores.'));
    process.exit(1);
  }
  log(c.gray(`  static analysis engine: ${roslyn ? 'Roslyn (precise)' : 'regex (fallback)'}`));

  // A run that was minimally patched to build/boot carries a `bench-patch.json` marker
  // ({points, reason}); we subtract the penalty so its real work is still scored, with the patch
  // documented in the report.
  let penalty: SubmissionReport['penalty'];
  const patchFile = path.join(dir, 'bench-patch.json');
  if (existsSync(patchFile)) {
    try {
      const p = JSON.parse(readFileSync(patchFile, 'utf8'));
      const points = Number(p.points) || 0;
      if (points > 0) {
        penalty = { points, reason: String(p.reason ?? 'patched to build/boot') };
        notes.push(`Patched to run (penalty -${points}): ${penalty.reason}`);
      }
    } catch {
      /* ignore a malformed marker */
    }
  }

  // Finalize a report and stamp the engine used (Roslyn vs regex fallback) before returning.
  const finish = (
    booted: boolean,
    checks: CheckResult[],
    notes: string[],
    stress?: SubmissionReport['stress'],
  ): SubmissionReport => {
    const r = finalizeReport(name, dir, booted, checks, notes, stress, penalty);
    r.engine = roslyn ? 'roslyn' : 'regex';
    return r;
  };

  checks.push(...runStaticChecks(files, roslyn));
  checks.push(...runArchitectureChecks(files, roslyn));
  checks.push(...runQualityChecks(files, roslyn));
  checks.push(...runKafkaStaticChecks(files, roslyn)); // kafka healthcheck + durability (static)

  // Informational notes from the precise analysis (do not affect the score).
  if (roslyn) {
    if (roslyn.useCases.length > 0 && !roslyn.oneFilePerUseCase)
      notes.push('Use cases are not one-class-per-file (prompt asks for one file per use case).');
    if (roslyn.useCaseTouchesDbContext)
      notes.push('A use case references the DbContext directly (should go through a repository).');
    if (roslyn.minimalApiResourceEndpoints > 0)
      notes.push(`Found ${roslyn.minimalApiResourceEndpoints} Minimal API resource endpoint(s) (prompt asks for controllers).`);
  }

  if (opts.staticOnly) {
    notes.push('static-only run: build/functional/kafka/stress were skipped.');
    return finish(false, checks, notes, stress);
  }

  // --- boot ---
  const composeFile = findComposeFile(dir);
  if (!composeFile) {
    notes.push('No docker-compose file found - cannot boot.');
    checks.push(check('build.up', 'build', 'docker compose up', WEIGHTS.build.composeUp, false, 'no compose file'));
    checks.push(check('build.healthy', 'build', 'API becomes healthy', WEIGHTS.build.healthy, false, 'no compose file'));
    checks.push(...functionalSkipped('no compose file'), ...kafkaSkipped('no compose file'), ...stressSkipped('no compose file'));
    return finish(false, checks, notes, stress);
  }

  if (!(await dockerAvailable())) {
    notes.push('Docker does not appear to be available/running.');
    checks.push(check('build.up', 'build', 'docker compose up', WEIGHTS.build.composeUp, false, 'docker unavailable'));
    checks.push(check('build.healthy', 'build', 'API becomes healthy', WEIGHTS.build.healthy, false, 'docker unavailable'));
    checks.push(...functionalSkipped('docker unavailable'), ...kafkaSkipped('docker unavailable'), ...stressSkipped('docker unavailable'));
    return finish(false, checks, notes, stress);
  }

  const project = projectName(name);
  const ports = [8080, 29092];
  const maxAttempts = 1 + Math.max(0, opts.retries);
  let up: CommandResult = { code: -1, stdout: '', stderr: '', timedOut: false };
  let upOk = false;
  let healthy = false;

  for (let attempt = 1; attempt <= maxAttempts && !healthy; attempt++) {
    if (attempt > 1) log(c.yellow(`  boot attempt ${attempt}/${maxAttempts} (retrying after a transient failure)`));
    log(c.gray('  cleaning project + stale benchmark containers on ports 8080/29092 ...'));
    await composeDown(dir, project, composeFile, { timeoutMs: config.docker.downTimeoutMs });
    await cleanupStaleBenchContainers(ports, (s) => log(c.gray('  ' + s)));

    // Diagnose external (non-benchmark) containers occupying the required ports.
    const external = (await portHolders(ports)).filter((h) => !h.container.startsWith('bench-'));
    if (external.length) {
      const msg = external.map((h) => `:${h.port} held by '${h.container}'`).join(', ');
      log(c.yellow(`  WARNING: required ports are busy with non-benchmark containers - ${msg}`));
      if (attempt === maxAttempts) notes.push(`Boot blocked: required ports in use by non-benchmark containers (${msg}). Stop them and re-run.`);
    }

    log(c.gray('  docker compose up --build ... (this can take a few minutes)'));
    up = await composeUp(dir, project, composeFile, { timeoutMs: config.docker.upTimeoutMs });
    upOk = up.code === 0 && !up.timedOut;
    if (!upOk) {
      const tail = (up.stderr || up.stdout).split('\n').slice(-6).join(' | ').slice(0, 400);
      log(c.gray(`  compose up failed: ${tail}`));
      continue;
    }

    log(c.gray('  waiting for the API to become healthy ...'));
    healthy = await waitForApi(config.api.bootTimeoutMs, (s) => log(c.gray('  ' + s)));
    if (!healthy) log(c.gray('  API did not become healthy'));
  }

  checks.push(check('build.up', 'build', 'docker compose up', WEIGHTS.build.composeUp, upOk,
    upOk ? `compose started${maxAttempts > 1 ? ` (within ${maxAttempts} attempts)` : ''}` : `exit=${up.code}${up.timedOut ? ' (timed out)' : ''}`));
  checks.push(check('build.healthy', 'build', 'API becomes healthy', WEIGHTS.build.healthy, healthy,
    healthy ? 'health OK' : `not healthy within ${config.api.bootTimeoutMs}ms`));

  if (!upOk || !healthy) {
    if (!upOk) {
      const tail = (up.stderr || up.stdout).split('\n').slice(-8).join(' | ').slice(0, 500);
      notes.push(`compose up failed after ${maxAttempts} attempt(s): ${tail}`);
    } else {
      notes.push(`API never became healthy within the boot timeout (after ${maxAttempts} attempt(s)).`);
    }
    const reason = upOk ? 'API not healthy' : 'boot failed';
    checks.push(...functionalSkipped(reason), ...kafkaSkipped(reason), ...stressSkipped(reason));
    if (!opts.keepUp) await composeDown(dir, project, composeFile, { timeoutMs: config.docker.downTimeoutMs });
    return finish(false, checks, notes, stress);
  }

  booted = true;

  // --- functional ---
  log(c.gray('  running functional tests ...'));
  checks.push(...(await runFunctionalChecks()));

  // --- kafka ---
  if (opts.noKafka) {
    notes.push('Kafka checks skipped (--no-kafka).');
  } else {
    log(c.gray('  checking Kafka publishing ...'));
    checks.push(...(await runKafkaChecks()));
  }

  // --- stress ---
  if (opts.noStress) {
    notes.push('Stress checks skipped (--no-stress).');
  } else {
    // Conservative median: stress is sensitive to host load, so if the thresholds are missed we
    // re-run it (no rebuild) to gather more samples, then pick the conservative-median attempt by
    // total earned - anti-optimistic, so transient *good* luck can't inflate the score either.
    const earned = (cs: CheckResult[]) => cs.reduce((s, c) => s + c.earned, 0);
    log(c.gray(`  stress test (${config.stress.concurrency} workers, ${config.stress.durationMs / 1000}s) ...`));
    const runs = [await runStressChecks()];
    const attempts = 1 + Math.max(0, opts.retries);
    for (let i = 1; i < attempts && runs[runs.length - 1].checks.some((c) => !c.passed); i++) {
      log(c.gray(`  stress thresholds missed - re-running stress (attempt ${i + 1}/${attempts}, conservative median) ...`));
      runs.push(await runStressChecks());
    }
    // Sort ascending by earned and pick the lower-middle index (for 2 runs this is the LOWER one).
    const sorted = [...runs].sort((a, b) => earned(a.checks) - earned(b.checks));
    const chosen = sorted[Math.floor((sorted.length - 1) / 2)];
    if (chosen.checks.some((c) => !c.passed))
      notes.push(`Stress did not fully pass (conservative median across ${runs.length} attempt(s)) - may indicate a loaded host or a real tail-latency issue.`);
    checks.push(...chosen.checks);
    stress = chosen.metrics;
  }

  // Opt-in runtime integrity check (must run while the containers are still up).
  let integrity: { passed: boolean; detail: string } | undefined;
  if (opts.strictDb) {
    log(c.gray('  verifying Postgres persistence (--strict-db) ...'));
    integrity = await verifyRuntimePostgres(project);
  }

  if (!opts.keepUp) {
    log(c.gray('  docker compose down ...'));
    await composeDown(dir, project, composeFile, { timeoutMs: config.docker.downTimeoutMs });
  } else {
    notes.push('Containers left running (--keep-up).');
  }

  const report = finish(booted, checks, notes, stress);
  report.integrity = integrity;
  return report;
}

function printSummary(r: SubmissionReport) {
  log('');
  log(c.bold(`  ${r.name}: ${r.totalEarned}/${r.totalMax} (${r.percent}%) ${r.booted ? '' : c.red('[did not boot]')}`));
  for (const cat of r.categories) {
    const full = cat.earned >= cat.max;
    const none = cat.earned <= 0;
    const color = full ? c.green : none ? c.red : c.yellow;
    log('   ' + color(`${cat.title.padEnd(28)} ${round1(cat.earned)}/${round1(cat.max)}`));
  }
  if (r.integrity) {
    const tag = r.integrity.passed ? c.green('strict-db: VERIFIED ✅') : c.red('strict-db: FAILED ❌');
    log('   ' + tag + c.gray(` - ${r.integrity.detail}`));
  }
}

async function main() {
  const opts = parseArgs(process.argv.slice(2));
  mkdirSync(RESULTS_DIR, { recursive: true });

  if (opts.leaderboard) {
    buildLeaderboardFromResults();
    return;
  }

  const all = listSubmissions();
  // A target may be a full submission ("gpt-5-5-xhigh/run1") or a bare model name ("gpt-5-5-xhigh"),
  // which expands to all of that model's runs.
  const expand = (t: string): string[] => {
    if (all.includes(t)) return [t];
    const runs = all.filter((s) => s.startsWith(t + '/'));
    return runs.length ? runs : [t]; // unknown stays, reported below
  };
  const targets = [...new Set(opts.names.length ? opts.names.flatMap(expand) : all)];

  if (!targets.length) {
    log(c.red(`No submissions found in ${SUBMISSIONS_DIR}`));
    process.exit(1);
  }

  const missing = targets.filter((t) => !all.includes(t));
  if (missing.length) {
    log(c.red(`Unknown submission(s): ${missing.join(', ')}`));
    log(c.gray(`Available: ${all.join(', ') || '(none)'}`));
    process.exit(1);
  }

  log(c.bold(`Benchmark evaluator - ${targets.length} submission(s): ${targets.join(', ')}`));

  const reports: SubmissionReport[] = [];
  for (const name of targets) {
    try {
      const r = await evaluate(name, opts);
      const { json, md } = writeReports(RESULTS_DIR, r);
      printSummary(r);
      log(c.gray(`   report: ${path.relative(repoRoot, md)}  |  ${path.relative(repoRoot, json)}`));
      reports.push(r);
    } catch (err) {
      log(c.red(`   failed to evaluate ${name}: ${String(err)}`));
    }
  }

  if (reports.length > 1) {
    const lb = writeLeaderboard(RESULTS_DIR, reports);
    log('');
    log(c.bold(`Leaderboard: ${path.relative(repoRoot, lb)}`));
    [...reports]
      .sort((a, b) => b.totalEarned - a.totalEarned)
      .forEach((r, i) => log(`  ${i + 1}. ${r.name.padEnd(24)} ${r.totalEarned}/${r.totalMax} (${r.percent}%)`));
  }

  log('');
  log(c.green('Done.'));
}

main().catch((err) => {
  log(c.red(String(err)));
  process.exit(1);
});
