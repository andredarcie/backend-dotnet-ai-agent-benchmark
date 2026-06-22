import { writeFileSync } from 'node:fs';
import path from 'node:path';
import { CATEGORY_TITLES } from './config';
import type { Category, CategoryScore, CheckResult, ModelGroup, SubmissionReport } from './types';
import { round1 } from './util';

const CATEGORY_ORDER: Category[] = ['static', 'architecture', 'build', 'functional', 'kafka', 'stress', 'quality'];

// Below this many runs per model, a ranking is statistically weak — flag it as provisional.
export const RECOMMENDED_RUNS = 5;

export function buildCategories(checks: CheckResult[]): CategoryScore[] {
  return CATEGORY_ORDER.map((category) => {
    const own = checks.filter((c) => c.category === category);
    return {
      category,
      title: CATEGORY_TITLES[category],
      earned: round1(own.reduce((s, c) => s + c.earned, 0)),
      max: round1(own.reduce((s, c) => s + c.weight, 0)),
      checks: own,
    };
  }).filter((c) => c.checks.length > 0);
}

export function finalizeReport(
  name: string,
  dir: string,
  booted: boolean,
  checks: CheckResult[],
  notes: string[],
  stress?: SubmissionReport['stress'],
): SubmissionReport {
  const categories = buildCategories(checks);
  const totalEarned = round1(categories.reduce((s, c) => s + c.earned, 0));
  const totalMax = round1(categories.reduce((s, c) => s + c.max, 0));
  return {
    name,
    path: dir,
    booted,
    categories,
    totalEarned,
    totalMax,
    percent: totalMax ? Math.round((totalEarned / totalMax) * 1000) / 10 : 0,
    stress,
    notes,
  };
}

function mark(passed: boolean): string {
  return passed ? '✅' : '❌';
}

export function renderMarkdown(r: SubmissionReport): string {
  const lines: string[] = [];
  lines.push(`# Benchmark report - \`${r.name}\``);
  lines.push('');
  lines.push(`**Score: ${r.totalEarned} / ${r.totalMax} (${r.percent}%)** - ${r.booted ? 'booted ✅' : 'did not boot ❌'}`);
  lines.push(`**Analysis engine:** \`${r.engine ?? 'unknown'}\``);
  lines.push('');

  if (r.integrity) {
    lines.push(`**Runtime integrity (strict-db):** ${r.integrity.passed ? 'VERIFIED ✅' : 'FAILED ❌'} - ${r.integrity.detail}`);
    lines.push('');
  }

  lines.push('| Category | Score |');
  lines.push('|----------|------:|');
  for (const cat of r.categories) {
    lines.push(`| ${cat.title} | ${cat.earned} / ${cat.max} |`);
  }
  lines.push(`| **Total** | **${r.totalEarned} / ${r.totalMax}** |`);
  lines.push('');

  if (r.stress) {
    const s = r.stress;
    lines.push('### Stress metrics');
    lines.push('');
    lines.push(`- Requests: **${s.totalRequests}** (${s.rps} req/s), errors: **${s.errors}** (${(s.errorRate * 100).toFixed(2)}%)`);
    lines.push(`- Latency: p50 **${s.p50}ms**, p95 **${s.p95}ms**, p99 **${s.p99}ms**`);
    lines.push('');
  }

  for (const cat of r.categories) {
    lines.push(`### ${cat.title} - ${cat.earned}/${cat.max}`);
    lines.push('');
    lines.push('| | Check | Pts | Detail |');
    lines.push('|--|-------|----:|--------|');
    for (const c of cat.checks) {
      lines.push(`| ${mark(c.passed)} | ${c.description} | ${round1(c.earned)}/${round1(c.weight)} | ${(c.detail ?? c.evidence ?? '').replace(/\|/g, '\\|')} |`);
    }
    lines.push('');
  }

  if (r.notes.length) {
    lines.push('### Notes');
    lines.push('');
    for (const n of r.notes) lines.push(`- ${n}`);
    lines.push('');
  }

  return lines.join('\n');
}

export function writeReports(resultsDir: string, r: SubmissionReport): { json: string; md: string } {
  const json = path.join(resultsDir, `${r.name}.json`);
  const md = path.join(resultsDir, `${r.name}.md`);
  writeFileSync(json, JSON.stringify(r, null, 2), 'utf8');
  writeFileSync(md, renderMarkdown(r), 'utf8');
  return { json, md };
}

/** Strip a `__<runId>` suffix to get the base model name (a folder `<model>__<runId>` is one run). */
export function baseModelName(name: string): string {
  return name.replace(/__.+$/, '');
}

/** Group submission reports into per-model groups, picking a conservative median run as representative. */
export function groupByModel(reports: SubmissionReport[]): ModelGroup[] {
  const byModel = new Map<string, SubmissionReport[]>();
  for (const r of reports) {
    const model = baseModelName(r.name);
    const arr = byModel.get(model);
    if (arr) arr.push(r);
    else byModel.set(model, [r]);
  }

  const groups: ModelGroup[] = [];
  for (const [model, runs] of byModel) {
    const sorted = [...runs].sort((a, b) => a.totalEarned - b.totalEarned);
    const n = sorted.length;
    const representative = sorted[Math.floor((n - 1) / 2)];
    const totals = sorted.map((r) => r.totalEarned);
    const meanTotal = totals.reduce((s, t) => s + t, 0) / n;
    // sample standard deviation (Bessel's n-1); 0 for a single run
    const stddev = n > 1 ? Math.sqrt(totals.reduce((s, t) => s + (t - meanTotal) ** 2, 0) / (n - 1)) : 0;
    groups.push({
      model,
      runs,
      n,
      medianTotal: representative.totalEarned,
      meanTotal: round1(meanTotal),
      stddev: round1(stddev),
      minTotal: sorted[0].totalEarned,
      maxTotal: sorted[n - 1].totalEarned,
      totalMax: representative.totalMax,
      representative,
    });
  }

  groups.sort((a, b) => b.medianTotal - a.medianTotal);
  return groups;
}

export function writeLeaderboard(resultsDir: string, reports: SubmissionReport[]): string {
  const groups = groupByModel(reports);
  const lines: string[] = [];
  lines.push('# Leaderboard');
  lines.push('');
  lines.push('| # | Model | Runs | Total | Static | Arch | Boot | Functional | Kafka | Stress | Quality |');
  lines.push('|--:|-------|-----:|------:|-------:|-----:|-----:|-----------:|------:|-------:|--------:|');

  const get = (r: SubmissionReport, cat: Category) => {
    const c = r.categories.find((x) => x.category === cat);
    return c ? `${c.earned}/${c.max}` : '-';
  };

  groups.forEach((g, i) => {
    const r = g.representative;
    const percent = g.totalMax ? Math.round((g.medianTotal / g.totalMax) * 1000) / 10 : 0;
    // Rank by median; report the spread (mean ± sample σ, range) so a tester sees the noise.
    const spread = g.n > 1 ? ` ±${g.stddev} · mean ${g.meanTotal} · ${g.minTotal}-${g.maxTotal}` : '';
    const runsCell = g.n < RECOMMENDED_RUNS ? `${g.n} ⚠` : `${g.n}`;
    const total = `**${g.medianTotal}/${g.totalMax}** (${percent}%)${spread}`;
    lines.push(
      `| ${i + 1} | \`${g.model}\` | ${runsCell} | ${total} | ` +
        `${get(r, 'static')} | ${get(r, 'architecture')} | ${get(r, 'build')} | ` +
        `${get(r, 'functional')} | ${get(r, 'kafka')} | ${get(r, 'stress')} | ${get(r, 'quality')} |`,
    );
  });
  lines.push('');

  if (groups.some((g) => g.n < RECOMMENDED_RUNS)) {
    lines.push(
      `> ⚠ **Provisional ranking** — models marked ⚠ have fewer than ${RECOMMENDED_RUNS} runs, so the ` +
        'totals are a weak sample (a one- or two-point gap is likely noise). Add more runs with ' +
        '`npm run add-run -- <model> <generated-project>` and re-run `npm run eval`.',
    );
    lines.push('');
  }

  const out = path.join(resultsDir, 'leaderboard.md');
  writeFileSync(out, lines.join('\n'), 'utf8');
  return out;
}
