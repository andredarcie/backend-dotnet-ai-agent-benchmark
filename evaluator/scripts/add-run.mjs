// Files a freshly-generated project as the next run of a model, so multi-run benchmarking is
// one command. Usage:  npm run add-run -- <model> <path-to-generated-project>
// It copies <path> into submissions/<model>/run<N+1>/ (skipping build dirs), where N is the
// highest run that model already has. Then evaluate with:  npm run eval -- <model>  (all runs)
// or  npm run eval -- <model>/run<N>  (one run).
import { existsSync, readdirSync, statSync, cpSync, mkdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const SKIP = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', '.idea', 'dist', '.vscode']);
const COMPOSE = ['docker-compose.yml', 'docker-compose.yaml', 'compose.yml', 'compose.yaml'];

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, '..', '..');
const SUBMISSIONS = path.join(repoRoot, 'submissions');

const fail = (msg) => {
  console.error(`add-run: ${msg}`);
  console.error('Usage: npm run add-run -- <model> <path-to-generated-project>');
  process.exit(1);
};

const [model, src] = process.argv.slice(2);
if (!model || !src) fail('missing arguments');
if (!/^[a-z0-9-]+$/.test(model)) fail(`model name must be kebab-case [a-z0-9-]: "${model}"`);

const srcAbs = path.resolve(src);
if (!existsSync(srcAbs) || !statSync(srcAbs).isDirectory()) fail(`source is not a directory: ${srcAbs}`);

const modelDir = path.join(SUBMISSIONS, model);
if (existsSync(modelDir) && COMPOSE.some((f) => existsSync(path.join(modelDir, f))))
  fail(`submissions/${model} is a flat project (compose at its root). Move it to submissions/${model}/run1/ first.`);

// Next run index = highest existing run<N> + 1 (so runs stay sequential).
let maxRun = 0;
if (existsSync(modelDir)) {
  for (const e of readdirSync(modelDir, { withFileTypes: true })) {
    const m = e.isDirectory() && e.name.match(/^run(\d+)$/);
    if (m) maxRun = Math.max(maxRun, parseInt(m[1], 10));
  }
}
const next = maxRun + 1;
const destAbs = path.join(modelDir, `run${next}`);
if (existsSync(destAbs)) fail(`submissions/${model}/run${next} already exists - remove it first`);

mkdirSync(modelDir, { recursive: true });
cpSync(srcAbs, destAbs, {
  recursive: true,
  filter: (p) => !path.relative(srcAbs, p).split(path.sep).some((seg) => SKIP.has(seg)),
});

const hasCompose = COMPOSE.some((f) => existsSync(path.join(destAbs, f)));
console.log(`✓ added submissions/${model}/run${next}  (run #${next} for "${model}")  ← ${srcAbs}`);
if (!hasCompose) console.warn('⚠ no docker-compose file at the run root - this run will fail to boot.');
console.log(`  next: npm run eval -- ${model}            (grade every run of this model)`);
console.log(`  or:   npm run eval -- ${model}/run${next}      (grade just this run)`);
console.log('  then: npm run eval -- --leaderboard');
