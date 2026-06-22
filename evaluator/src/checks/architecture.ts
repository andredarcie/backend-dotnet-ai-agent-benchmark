import { WEIGHTS } from '../config';
import { anyMatch, csFiles, stripComments, type SourceFile } from '../files';
import type { RoslynResult } from '../roslyn';
import type { CheckResult } from '../types';
import { check } from '../util';

const W = WEIGHTS.architecture;

export function runArchitectureChecks(files: SourceFile[], roslyn: RoslynResult | null): CheckResult[] {
  const results: CheckResult[] = [];
  const cs = csFiles(files);
  const via = roslyn ? ' [roslyn]' : ' [regex]';

  if (roslyn) {
    results.push(check('arch.repository', 'architecture', 'Repository layer (interface + implementation)' + via,
      W.repositoryLayer, roslyn.hasRepositoryInterface && roslyn.hasRepositoryImpl,
      `interface=${roslyn.hasRepositoryInterface}, impl=${roslyn.hasRepositoryImpl}`));

    results.push(check('arch.baseRepository', 'architecture', 'Generic base repository class' + via,
      W.baseRepository, roslyn.baseRepository));

    results.push(check('arch.useCases', 'architecture', 'Use-case layer (*UseCase classes)' + via,
      W.useCaseLayer, roslyn.useCases.length > 0, `${roslyn.useCases.length} use case(s)`));

    // The Roslyn check is call-graph aware: controllers must reference a use case AND must
    // NOT touch the DbContext directly (comments are ignored - it's the syntax tree).
    results.push(check('arch.controllerUseCase', 'architecture', 'Controllers call use cases (not DbContext)' + via,
      W.controllersUseUseCases, roslyn.controllerUsesUseCase && !roslyn.controllerTouchesDbContext,
      `usesUseCase=${roslyn.controllerUsesUseCase}, touchesDb=${roslyn.controllerTouchesDbContext}`));

    results.push(check('arch.repoEf', 'architecture', 'Repositories own EF Core / DbContext access' + via,
      W.repositoriesOwnEf, roslyn.repoUsesEf));

    return results;
  }

  // --- regex fallback ---
  const repoInterface = anyMatch(cs, /interface\s+I\w*Repository\b/);
  const repoClass = anyMatch(cs, /class\s+\w*Repository\b/);
  results.push(check('arch.repository', 'architecture', 'Repository layer (interface + implementation)' + via,
    W.repositoryLayer, !!repoInterface && !!repoClass, `interface=${!!repoInterface}, class=${!!repoClass}`));

  const baseRepo =
    anyMatch(cs, /abstract\s+class\s+\w*Repository\w*/) || anyMatch(cs, /\bRepositoryBase\b/) ||
    anyMatch(cs, /class\s+\w*Repository\w*<\s*\w/) || anyMatch(cs, /:\s*RepositoryBase</);
  results.push(check('arch.baseRepository', 'architecture', 'Generic base repository class' + via, W.baseRepository, !!baseRepo));

  const useCase = anyMatch(cs, /class\s+\w*(UseCase|Interactor)\b/);
  results.push(check('arch.useCases', 'architecture', 'Use-case layer (*UseCase classes)' + via, W.useCaseLayer, !!useCase));

  const controllerFiles = cs.filter((f) => /class\s+\w+Controller\b/.test(f.content)).map((f) => stripComments(f.content));
  const ctrlUsesUseCase = controllerFiles.some((src) => /(UseCase|Interactor)\b/.test(src));
  const ctrlTouchesDb = controllerFiles.some((src) => /\b(DbContext|AppDbContext)\b|DbSet</.test(src));
  results.push(check('arch.controllerUseCase', 'architecture', 'Controllers call use cases (not DbContext)' + via,
    W.controllersUseUseCases, controllerFiles.length > 0 && ctrlUsesUseCase && !ctrlTouchesDb,
    `usesUseCase=${ctrlUsesUseCase}, touchesDb=${ctrlTouchesDb}`));

  const repoFiles = cs.filter((f) => /class\s+\w*Repository\b/.test(f.content));
  const reposOwnEf = repoFiles.some((f) => /\b(DbContext|AppDbContext|DbSet<|Microsoft\.EntityFrameworkCore)\b|\bSet\b/.test(f.content));
  results.push(check('arch.repoEf', 'architecture', 'Repositories own EF Core / DbContext access' + via,
    W.repositoriesOwnEf, repoFiles.length > 0 && reposOwnEf));

  return results;
}
