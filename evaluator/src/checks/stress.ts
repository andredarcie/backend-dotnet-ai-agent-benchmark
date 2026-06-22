import { config, WEIGHTS } from '../config';
import { http } from '../http';
import type { CheckResult, StressMetrics } from '../types';
import { check } from '../util';

const W = WEIGHTS.stress;

/**
 * Drives concurrent read/write load against the API for a fixed window and scores three
 * coarse thresholds (error rate, throughput, p95 latency).
 *
 * These thresholds are a CORRECTNESS FLOOR, not a fine-grained discriminator: most working
 * APIs clear them comfortably, so they mainly catch APIs that fall over under load rather
 * than ranking healthy ones against each other. To smooth out per-run noise the orchestrator
 * runs this several times and selects the conservative MEDIAN across attempts (that median
 * selection lives in index.ts, not here). This function just returns one attempt's
 * { checks, metrics }.
 */
export async function runStressChecks(): Promise<{ checks: CheckResult[]; metrics: StressMetrics }> {
  // Seed a credit card so write load has a valid FK.
  const seed = await http('POST', '/api/credit-cards', {
    cardholderName: 'Stress Test', cardNumber: '4222222222222222', brand: 'VISA', creditLimit: 1_000_000,
  });
  const cardId: number | undefined = typeof seed.body?.id === 'number' ? seed.body.id : undefined;

  const end = Date.now() + config.stress.durationMs;
  const latencies: number[] = [];
  let total = 0;
  let errors = 0;
  let counter = 0;

  const worker = async () => {
    while (Date.now() < end) {
      const n = counter++;
      const start = Date.now();
      let failed = false;
      try {
        // ~1 in 4 requests is a write (transaction create); the rest are reads.
        const r =
          n % 4 === 0 && cardId !== undefined
            ? await http('POST', '/api/transactions', {
                creditCardId: cardId, amount: 10 + (n % 90), merchant: `Stress-${n}`, category: 'load',
              })
            : await http('GET', '/api/transactions');
        failed = !r.ok || r.status >= 500 || r.status === 0;
      } catch {
        failed = true;
      }
      latencies.push(Date.now() - start);
      total++;
      if (failed) errors++;
    }
  };

  await Promise.all(Array.from({ length: config.stress.concurrency }, () => worker()));

  latencies.sort((x, y) => x - y);
  const pct = (p: number) =>
    latencies.length ? latencies[Math.min(latencies.length - 1, Math.floor((p / 100) * latencies.length))] : 0;

  const errorRate = total ? errors / total : 1;
  const metrics: StressMetrics = {
    totalRequests: total,
    errors,
    errorRate,
    rps: Math.round((total / (config.stress.durationMs / 1000)) * 10) / 10,
    p50: pct(50),
    p95: pct(95),
    p99: pct(99),
  };

  const checks: CheckResult[] = [
    check('stress.errorRate', 'stress', `Error rate < ${config.stress.maxErrorRate * 100}%`, W.errorRate,
      total > 0 && errorRate < config.stress.maxErrorRate, `errorRate=${(errorRate * 100).toFixed(2)}% (${errors}/${total})`),
    check('stress.throughput', 'stress', `Sustained throughput ≥ ${config.stress.minRps} req/s`, W.throughput,
      metrics.rps >= config.stress.minRps, `${metrics.rps} req/s, ${total} total`),
    check('stress.p95', 'stress', `p95 latency < ${config.stress.maxP95Ms}ms`, W.p95,
      total > 0 && metrics.p95 < config.stress.maxP95Ms, `p95=${metrics.p95}ms`),
  ];

  return { checks, metrics };
}
