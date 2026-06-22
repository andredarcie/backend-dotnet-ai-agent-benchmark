import type { Category, CheckResult } from './types';

export const sleep = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));

export function check(
  id: string,
  category: Category,
  description: string,
  weight: number,
  passed: boolean,
  detail?: string,
  evidence?: string,
): CheckResult {
  return { id, category, description, weight, passed, earned: passed ? weight : 0, detail, evidence };
}

// Minimal ANSI colors (no dependency).
const useColor = process.stdout.isTTY && !process.env.NO_COLOR;
const wrap = (code: number) => (s: string | number) => (useColor ? `\x1b[${code}m${s}\x1b[0m` : String(s));
export const c = {
  bold: wrap(1),
  dim: wrap(2),
  red: wrap(31),
  green: wrap(32),
  yellow: wrap(33),
  cyan: wrap(36),
  gray: wrap(90),
};

export const round1 = (n: number) => Math.round(n * 10) / 10;
