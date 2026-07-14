using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>
/// The category evaluators, in report order.
///
/// <b>1–8 are SCORED</b> and their weights sum to 1.00. They are the engineering core: what the running
/// system proves (correctness, the HTTP contract, persistence, the real Kafka event), plus how the source
/// holds up (security/PCI, architecture, code quality, resilience).
///
/// <b>9–11 are INFORMATIONAL (Weight = 0)</b>: still measured and printed, but excluded from the score
/// (see <see cref="Scoring.Aggregate"/>). They were 1–4% categories that could never separate two
/// submissions — a 1%-weight Documentation score moves the final by 0.05, less than the run-to-run noise
/// of the same model. Reporting them is honest; ranking on them was not.
/// </summary>
public static class EvaluatorRegistry
{
    public static IReadOnlyList<ICategoryEvaluator> All { get; } = new ICategoryEvaluator[]
    {
        // ── scored (weights sum to 1.00) ──
        new FunctionalCorrectnessEvaluator(),   // 20% — the live oracle + the submission's own suite
        new ArchitectureEvaluator(),            // 12%
        new CodeQualityEvaluator(),             // 10%
        new ApiDesignEvaluator(),               // 14%
        new PersistenceEvaluator(),             // 13%
        new MessagingEvaluator(),               // 13%
        new SecurityEvaluator(),                // 14%
        new ResilienceEvaluator(),              //  4%
        // ── informational (weight 0, not ranked) ──
        new ObservabilityEvaluator(),
        new PortabilityEvaluator(),
        new DocumentationEvaluator(),
    };
}
