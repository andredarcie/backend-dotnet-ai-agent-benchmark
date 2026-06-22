export type Category =
  | 'static'
  | 'architecture'
  | 'build'
  | 'functional'
  | 'kafka'
  | 'stress'
  | 'quality';

export interface CheckResult {
  id: string;
  category: Category;
  description: string;
  weight: number;
  passed: boolean;
  earned: number;
  detail?: string;
  evidence?: string;
}

export interface CategoryScore {
  category: Category;
  title: string;
  earned: number;
  max: number;
  checks: CheckResult[];
}

export interface StressMetrics {
  totalRequests: number;
  errors: number;
  errorRate: number;
  rps: number;
  p50: number;
  p95: number;
  p99: number;
}

export interface SubmissionReport {
  name: string;
  path: string;
  booted: boolean;
  categories: CategoryScore[];
  totalEarned: number;
  totalMax: number;
  percent: number;
  stress?: StressMetrics;
  // Which static-analysis engine produced this report.
  engine?: 'roslyn' | 'regex';
  // Opt-in (--strict-db) runtime verdict; does not affect the 0-100 score.
  integrity?: { passed: boolean; detail: string };
  notes: string[];
}

export interface ModelGroup {
  model: string;
  runs: SubmissionReport[];
  n: number;
  medianTotal: number;
  meanTotal: number;
  stddev: number; // sample standard deviation of the run totals (0 when n < 2)
  minTotal: number;
  maxTotal: number;
  totalMax: number;
  representative: SubmissionReport;
}
