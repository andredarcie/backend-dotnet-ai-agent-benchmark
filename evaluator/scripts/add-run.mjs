// Files a freshly-generated project as the next run of a model, so multi-run benchmarking is
// one command. Usage:  npm run add-run -- <model> <path-to-generated-project>
// It copies <path> into submissions/<model>__run<N+1>/ (skipping build dirs), where N is how many
// runs that model already has. Then evaluate with:  npm run eval -- <model>__run<N>  (or all).
import { existsSync, readdirSync, statSync, cpSync } from 'node:fs';
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

// Count runs already grouped under this model (a bare `<model>` folder counts as one run too).
const baseName = (n) => n.replace(/__.+$/, '');
const existing = existsSync(SUBMISSIONS)
  ? readdirSync(SUBMISSIONS, { withFileTypes: true }).filter((e) => e.isDirectory() && baseName(e.name) === model)
  : [];
const next = existing.length + 1;
const destName = `${model}__run${next}`;
const destAbs = path.join(SUBMISSIONS, destName);
if (existsSync(destAbs)) fail(`${destName} already exists — remove it first`);

cpSync(srcAbs, destAbs, {
  recursive: true,
  filter: (p) => !path.relative(srcAbs, p).split(path.sep).some((seg) => SKIP.has(seg)),
});

const hasCompose = COMPOSE.some((f) => existsSync(path.join(destAbs, f)));
console.log(`✓ added submissions/${destName}  (run #${next} for "${model}")  ← ${srcAbs}`);
if (!hasCompose) console.warn('⚠ no docker-compose file at the folder root — this run will fail to boot.');
console.log(`  next: npm run eval -- ${destName}    (or: npm run eval  to run every submission)`);
console.log('  then: npm run eval -- --leaderboard');
