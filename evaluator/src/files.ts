import { readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';

const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', '.idea', 'dist', '.vscode']);
const TEXT_EXT = new Set([
  '.cs', '.csproj', '.fs', '.fsproj', '.vb', '.vbproj',
  '.yml', '.yaml', '.json', '.props', '.targets', '.sln', '.editorconfig', '.env', '.sh',
]);

export interface SourceFile {
  rel: string;
  abs: string;
  name: string;
  content: string;
}

export function readSourceFiles(root: string): SourceFile[] {
  const out: SourceFile[] = [];

  const walk = (dir: string) => {
    let entries;
    try {
      entries = readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }
    for (const e of entries) {
      const full = path.join(dir, e.name);
      if (e.isDirectory()) {
        if (SKIP_DIRS.has(e.name)) continue;
        walk(full);
      } else if (e.isFile()) {
        const ext = path.extname(e.name).toLowerCase();
        const isDockerfile = e.name.toLowerCase().startsWith('dockerfile');
        if (TEXT_EXT.has(ext) || isDockerfile) {
          try {
            out.push({
              rel: path.relative(root, full),
              abs: full,
              name: e.name,
              content: readFileSync(full, 'utf8'),
            });
          } catch {
            /* ignore unreadable files */
          }
        }
      }
    }
  };

  walk(root);
  return out;
}

// --- small helpers used by the static/architecture checks ---

export function composeFiles(files: SourceFile[]): SourceFile[] {
  return files.filter((f) => /(^|\/)(docker-compose|compose)\.(yml|yaml)$/i.test(f.rel.replace(/\\/g, '/')));
}

export function csFiles(files: SourceFile[]): SourceFile[] {
  return files.filter((f) => f.name.toLowerCase().endsWith('.cs'));
}

export function anyMatch(files: SourceFile[], re: RegExp): SourceFile | undefined {
  return files.find((f) => re.test(f.content));
}

/** Counts non-blank, non-comment lines of C# code (skips generated Migrations). */
export function countCodeLines(files: SourceFile[]): number {
  let total = 0;
  for (const f of files) {
    if (!f.name.toLowerCase().endsWith('.cs')) continue;
    if (/(^|[\\/])Migrations[\\/]/.test(f.rel)) continue;
    let inBlock = false;
    for (const raw of f.content.split('\n')) {
      let line = raw.trim();
      if (inBlock) {
        const end = line.indexOf('*/');
        if (end < 0) continue;
        line = line.slice(end + 2).trim();
        inBlock = false;
      }
      if (!line || line.startsWith('//') || line.startsWith('*')) continue;
      if (line.startsWith('/*')) {
        if (line.indexOf('*/') < 0) inBlock = true;
        continue;
      }
      total++;
    }
  }
  return total;
}

/** Removes // line comments and block comments so tokens in comments don't trigger checks. */
export function stripComments(src: string): string {
  return src.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/\/\/[^\n]*/g, ' ');
}

/** Distinct capture-group-1 values across all files for a global regex. */
export function distinctCaptures(files: SourceFile[], re: RegExp): Set<string> {
  const set = new Set<string>();
  for (const f of files) {
    for (const m of f.content.matchAll(re)) {
      if (m[1]) set.add(m[1]);
    }
  }
  return set;
}
