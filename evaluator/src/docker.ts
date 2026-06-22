import { spawn } from 'node:child_process';
import { existsSync } from 'node:fs';
import path from 'node:path';

export interface CommandResult {
  code: number;
  stdout: string;
  stderr: string;
  timedOut: boolean;
}

const COMPOSE_FILES = ['docker-compose.yml', 'docker-compose.yaml', 'compose.yml', 'compose.yaml'];

export function findComposeFile(dir: string): string | null {
  return COMPOSE_FILES.find((f) => existsSync(path.join(dir, f))) ?? null;
}

export function projectName(submission: string): string {
  return 'bench-' + submission.toLowerCase().replace(/[^a-z0-9_-]/g, '-');
}

function run(
  cmd: string,
  args: string[],
  opts: { cwd: string; timeoutMs?: number; onLog?: (s: string) => void; input?: string },
): Promise<CommandResult> {
  return new Promise((resolve) => {
    const child = spawn(cmd, args, { cwd: opts.cwd, shell: process.platform === 'win32' });
    let stdout = '';
    let stderr = '';
    let timedOut = false;
    if (opts.input !== undefined && child.stdin) {
      child.stdin.write(opts.input);
      child.stdin.end();
    }
    const timer = opts.timeoutMs
      ? setTimeout(() => {
          timedOut = true;
          child.kill();
        }, opts.timeoutMs)
      : null;
    child.stdout.on('data', (d) => {
      const s = d.toString();
      stdout += s;
      opts.onLog?.(s);
    });
    child.stderr.on('data', (d) => {
      const s = d.toString();
      stderr += s;
      opts.onLog?.(s);
    });
    child.on('close', (code) => {
      if (timer) clearTimeout(timer);
      resolve({ code: code ?? -1, stdout, stderr, timedOut });
    });
    child.on('error', (err) => {
      if (timer) clearTimeout(timer);
      resolve({ code: -1, stdout, stderr: stderr + String(err), timedOut });
    });
  });
}

export function composeUp(
  dir: string,
  project: string,
  composeFile: string,
  opts: { timeoutMs: number; onLog?: (s: string) => void },
): Promise<CommandResult> {
  return run('docker', ['compose', '-p', project, '-f', composeFile, 'up', '-d', '--build'], {
    cwd: dir,
    timeoutMs: opts.timeoutMs,
    onLog: opts.onLog,
  });
}

export function composeDown(
  dir: string,
  project: string,
  composeFile: string,
  opts: { timeoutMs: number; onLog?: (s: string) => void },
): Promise<CommandResult> {
  return run('docker', ['compose', '-p', project, '-f', composeFile, 'down', '-v', '--remove-orphans'], {
    cwd: dir,
    timeoutMs: opts.timeoutMs,
    onLog: opts.onLog,
  });
}

/** Run an arbitrary docker command (for ps/inspect/exec), optionally feeding stdin. */
export function dockerCmd(args: string[], opts: { timeoutMs?: number; input?: string } = {}): Promise<CommandResult> {
  return run('docker', args, { cwd: process.cwd(), timeoutMs: opts.timeoutMs ?? 30_000, input: opts.input });
}

/**
 * Removes leftover **benchmark** containers (named `bench-*`) that still hold the given host
 * ports — orphans from a crashed/killed prior run. Scoped to `bench-*` names so it never
 * touches the user's unrelated containers.
 */
export async function cleanupStaleBenchContainers(ports: number[], onLog?: (s: string) => void): Promise<void> {
  for (const port of ports) {
    const ps = await dockerCmd(['ps', '-aq', '--filter', `publish=${port}`, '--filter', 'name=bench-']);
    const ids = ps.stdout.split('\n').map((s) => s.trim()).filter(Boolean);
    for (const id of ids) {
      onLog?.(`removing stale bench container ${id} holding port ${port}`);
      await dockerCmd(['rm', '-f', id]);
    }
  }
}

/** Returns the names of running containers currently publishing any of the given host ports. */
export async function portHolders(ports: number[]): Promise<{ port: number; container: string }[]> {
  const out: { port: number; container: string }[] = [];
  for (const port of ports) {
    const ps = await dockerCmd(['ps', '--filter', `publish=${port}`, '--format', '{{.Names}}']);
    for (const name of ps.stdout.split('\n').map((s) => s.trim()).filter(Boolean)) out.push({ port, container: name });
  }
  return out;
}

export async function dockerAvailable(): Promise<boolean> {
  const res = await run('docker', ['version', '--format', '{{.Server.Version}}'], {
    cwd: process.cwd(),
    timeoutMs: 15_000,
  });
  return res.code === 0;
}
