import { config, WEIGHTS } from '../config';
import { http } from '../http';
import type { CheckResult, StressMetrics } from '../types';
import { check } from '../util';

const W = WEIGHTS.stress;

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
