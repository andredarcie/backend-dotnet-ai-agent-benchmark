import { dockerCmd } from '../docker';

export interface IntegrityResult {
  passed: boolean;
  detail: string;
}

/**
 * Opt-in (--strict-db) runtime integrity check: confirms the submission actually persisted
 * its schema to PostgreSQL (not an in-memory provider with an unused Npgsql reference).
 *
 * It finds the Postgres container for this compose project, reads its credentials, and counts
 * the base tables in the public schema via psql. An in-memory app leaves Postgres empty.
 *
 * Identifier-only args avoid shell-quoting issues; the SQL is fed over stdin.
 */
export async function verifyRuntimePostgres(project: string): Promise<IntegrityResult> {
  // 1. Find the containers belonging to this compose project.
  const ps = await dockerCmd(['ps', '--filter', `label=com.docker.compose.project=${project}`, '--format', '{{.ID}}']);
  const ids = ps.stdout.split('\n').map((l) => l.trim()).filter(Boolean);
  if (!ids.length) return { passed: false, detail: `no running containers found for project ${project}` };

  // 2. Pick the one whose image looks like Postgres.
  let pgId = '';
  for (const id of ids) {
    const img = await dockerCmd(['inspect', id, '--format', '{{.Config.Image}}']);
    if (/postgres/i.test(img.stdout)) {
      pgId = id;
      break;
    }
  }
  if (!pgId) return { passed: false, detail: 'no postgres container found in the compose project' };

  // 3. Read the DB credentials from the container's env (printenv → no arg spaces).
  const userRes = await dockerCmd(['exec', pgId, 'printenv', 'POSTGRES_USER']);
  const dbRes = await dockerCmd(['exec', pgId, 'printenv', 'POSTGRES_DB']);
  const user = (userRes.code === 0 && userRes.stdout.trim()) || 'postgres';
  const db = (dbRes.code === 0 && dbRes.stdout.trim()) || user;

  if (!/^[A-Za-z0-9_]+$/.test(user) || !/^[A-Za-z0-9_]+$/.test(db)) {
    return { passed: false, detail: `unexpected pg identifiers (user="${user}", db="${db}")` };
  }

  // 4. Count base tables in the public schema (SQL via stdin to dodge shell quoting).
  const sql = "SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';";
  const res = await dockerCmd(['exec', '-i', pgId, 'psql', '-U', user, '-d', db, '-tA'], { input: sql });
  const match = res.stdout.match(/\d+/);
  const tables = match ? parseInt(match[0], 10) : NaN;

  if (!Number.isFinite(tables)) {
    return { passed: false, detail: `could not read table count from Postgres(${db}); psql exit ${res.code}: ${(res.stderr || res.stdout).trim().slice(0, 200)}` };
  }

  const passed = tables >= 2;
  return {
    passed,
    detail: passed
      ? `Postgres(${db}) holds ${tables} base tables → schema was persisted to a real Postgres`
      : `Postgres(${db}) holds only ${tables} base tables (expected ≥ 2) → the API likely did not use Postgres`,
  };
}
