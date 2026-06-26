using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>
/// Ordered list of the 13 category evaluators.
/// Weights mirror EVALUATION-CRITERIA.md and sum to 1.00.
/// </summary>
public static class EvaluatorRegistry
{
    public static IReadOnlyList<ICategoryEvaluator> All { get; } = new ICategoryEvaluator[]
    {
        new FunctionalCorrectnessEvaluator(),
        new ArchitectureEvaluator(),
        new CodeQualityEvaluator(),
        new ApiDesignEvaluator(),
        new PersistenceEvaluator(),
        new MessagingEvaluator(),
        new SecurityEvaluator(),
        new ResilienceEvaluator(),
        new TestsEvaluator(),
        new ObservabilityEvaluator(),
        new PerformanceEvaluator(),
        new PortabilityEvaluator(),
        new DocumentationEvaluator(),
    };
}
