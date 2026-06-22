import { spawn } from 'node:child_process';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const srcDir = path.dirname(fileURLToPath(import.meta.url));
const analyzerDir = path.resolve(srcDir, '..', 'analyzer');
const analyzerDll = path.join(analyzerDir, 'bin', 'Release', 'net10.0', 'Analyzer.dll');

export interface RoslynResult {
  ok: boolean;
  targetFrameworks: string[];
  controllers: string[];
  dbContexts: string[];
  entities: string[];
  usesEfNamespace: boolean;
  useNpgsql: boolean;
  relationship: boolean;
  kafkaClient: boolean;
  kafkaProduce: boolean;
  hasRepositoryInterface: boolean;
  hasRepositoryImpl: boolean;
  baseRepository: boolean;
  useCases: string[];
  oneFilePerUseCase: boolean;
  controllerUsesUseCase: boolean;
  controllerTouchesDbContext: boolean;
  useCaseTouchesDbContext: boolean;
  repoUsesEf: boolean;
  minimalApiResourceEndpoints: number;
  controllersUseCancellation: boolean;
  reposUseCancellation: boolean;
  responseDtoTypes: string[];
  controllersUseDtos: boolean;
  useCasesReturnDtos: boolean;
  usesExceptionHandler: boolean;
  usesProblemDetails: boolean;
  usesResultPattern: boolean;
  databaseMigrate: boolean;
  kafkaDurable: boolean;
  kafkaPublishResilient: boolean;
  kafkaPublishOutbox: boolean;
}

interface ExecResult {
  code: number;
  stdout: string;
  stderr: string;
}

function exec(cmd: string, args: string[], timeoutMs: number): Promise<ExecResult> {
  return new Promise((resolve) => {
    const useShell = process.platform === 'win32';
    const finalArgs = useShell ? args.map((a) => (/\s/.test(a) ? `"${a}"` : a)) : args;
    const child = spawn(cmd, finalArgs, { cwd: analyzerDir, shell: useShell });
    let stdout = '';
    let stderr = '';
    const timer = setTimeout(() => child.kill(), timeoutMs);
    child.stdout.on('data', (d) => (stdout += d.toString()));
    child.stderr.on('data', (d) => (stderr += d.toString()));
    child.on('close', (code) => {
      clearTimeout(timer);
      resolve({ code: code ?? -1, stdout, stderr });
    });
    child.on('error', (err) => {
      clearTimeout(timer);
      resolve({ code: -1, stdout, stderr: stderr + String(err) });
    });
  });
}

let dotnetOk: boolean | null = null;
let buildState: 'unknown' | 'ok' | 'failed' = 'unknown';

async function dotnetAvailable(): Promise<boolean> {
  if (dotnetOk !== null) return dotnetOk;
  const r = await exec('dotnet', ['--version'], 20_000);
  dotnetOk = r.code === 0;
  return dotnetOk;
}

async function ensureBuilt(onLog?: (s: string) => void): Promise<boolean> {
  if (existsSync(analyzerDll)) return true;
  if (buildState === 'failed') return false;
  onLog?.('building Roslyn analyzer (one-time)...');
  const r = await exec('dotnet', ['build', '-c', 'Release', '-v', 'quiet'], 300_000);
  buildState = existsSync(analyzerDll) ? 'ok' : 'failed';
  if (buildState === 'failed') onLog?.(`analyzer build failed: ${(r.stderr || r.stdout).slice(-300)}`);
  return buildState === 'ok';
}

/**
 * Runs the Roslyn analyzer on a submission. Returns precise structural facts, or `null` if
 * dotnet/the analyzer is unavailable (callers then fall back to regex heuristics).
 */
export async function analyzeWithRoslyn(submissionDir: string, onLog?: (s: string) => void): Promise<RoslynResult | null> {
  if (!(await dotnetAvailable())) {
    onLog?.('dotnet not available - falling back to regex static analysis');
    return null;
  }
  if (!(await ensureBuilt(onLog))) return null;

  const r = await exec('dotnet', [analyzerDll, submissionDir], 120_000);
  const line = r.stdout.split('\n').map((l) => l.trim()).filter(Boolean).reverse().find((l) => l.startsWith('{'));
  if (!line) {
    onLog?.('analyzer produced no JSON - falling back to regex');
    return null;
  }
  try {
    const parsed = JSON.parse(line) as RoslynResult & { error?: string };
    if (!parsed.ok) {
      onLog?.(`analyzer error (${parsed.error ?? 'unknown'}) - falling back to regex`);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}
