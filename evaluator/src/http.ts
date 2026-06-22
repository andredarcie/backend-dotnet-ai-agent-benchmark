import { config } from './config';
import { sleep } from './util';

export interface HttpResponse {
  status: number;
  ok: boolean;
  body: any;
  raw: string;
  headers: Record<string, string>;
  error?: string;
}

export async function http(
  method: string,
  pathname: string,
  body?: unknown,
  baseUrl: string = config.api.baseUrl,
): Promise<HttpResponse> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), config.api.requestTimeoutMs);
  try {
    const res = await fetch(baseUrl + pathname, {
      method,
      headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal: controller.signal,
    });
    const raw = await res.text();
    let parsed: any = null;
    if (raw) {
      try {
        parsed = JSON.parse(raw);
      } catch {
        parsed = raw;
      }
    }
    const headers: Record<string, string> = {};
    res.headers.forEach((v, k) => {
      headers[k.toLowerCase()] = v;
    });
    return { status: res.status, ok: res.ok, body: parsed, raw, headers };
  } catch (err) {
    return { status: 0, ok: false, body: null, raw: '', headers: {}, error: String(err) };
  } finally {
    clearTimeout(timer);
  }
}

/** Polls until the API answers health (or any non-5xx), or the timeout elapses. */
export async function waitForApi(timeoutMs: number, onLog?: (s: string) => void): Promise<boolean> {
  const deadline = Date.now() + timeoutMs;
  let attempt = 0;
  while (Date.now() < deadline) {
    attempt++;
    const health = await http('GET', config.api.healthPath);
    if (health.status === 200) return true;
    const probe = await http('GET', '/api/credit-cards');
    if (probe.status > 0 && probe.status < 500) return true;
    if (attempt % 5 === 0) onLog?.(`still waiting for API (${attempt} tries)...`);
    await sleep(2000);
  }
  return false;
}
