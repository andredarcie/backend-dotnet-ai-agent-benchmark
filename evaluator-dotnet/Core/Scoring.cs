namespace BackendEvaluator.Core;

/// <summary>
/// Pure aggregation math for the final report. Per-category scoring lives on
/// <see cref="CategoryResult.Score"/> (weighted mean of measured metrics × 5); this folds the scored
/// categories into the weighted final, <b>renormalized over the categories that produced a score</b>
/// so unmeasured categories neither help nor hurt, and reports the coverage fraction.
/// </summary>
public static class Scoring
{
    public static (double? weightedScore, double coverage) Aggregate(IReadOnlyList<CategoryResult> categories)
    {
        var scored = categories.Where(c => c.Score.HasValue).ToList();
        double wsum = scored.Sum(c => c.Weight);
        double? weighted = wsum > 0
            ? Math.Round(scored.Sum(c => c.Score!.Value * c.Weight) / wsum, 2)
            : null;
        double coverage = categories.Count == 0 ? 0 : (double)scored.Count / categories.Count;
        return (weighted, coverage);
    }

    // A submission that doesn't compile or run cannot be "production-grade" however clean its source
    // reads — so quality alone must not earn a high score. These caps make the headline honest.
    //
    // THE RUN IS THE MEASUREMENT. A deep evaluation must EXERCISE the running system: the live oracle
    // (real 201/Location/400/404), a real event on the topic, DAST and load are what actually prove the
    // project works. Static signals ("the code contains an Outbox class / a Polly policy / the right
    // attributes") only show it LOOKS right. So a deep run that never booted has demonstrated nothing —
    // and, because unmeasured metrics are excluded (not failed), it would otherwise keep a near-perfect
    // static score. We refuse to certify that: no live boot ⇒ a grave cap.
    // The caps form a monotonic severity gradient keyed on how far the submission got: the less it ran,
    // the harder the cap. Whoever doesn't even compile is punished the most, because they demonstrated the
    // least — didn't even produce buildable code < compiled but shipped no runnable system < ran but never
    // became healthy.
    public const double BuildFailCap = 0.5;         // dotnet build failed: source does not compile — the gravest failure
    public const double NoRunnableSystemCap = 1.0;  // compiles but no docker-compose.yml: no runnable system was delivered
    public const double BootFailCap = 1.5;          // has a compose but never became healthy / was never verified running

    /// <summary>
    /// Caps the weighted score when the submission fails the executability gate. Precedence: build failure
    /// (<see cref="BuildFailCap"/>) ⇒ no runnable system (<see cref="NoRunnableSystemCap"/>) ⇒ boot/verify
    /// failure (<see cref="BootFailCap"/>). The boot gate applies only to <paramref name="deep"/> runs
    /// (light mode is static-only by design and is excluded from the ranked leaderboard). A deep run is
    /// gated unless it was actually observed healthy (<paramref name="boots"/> == true) — an unknown boot
    /// (null, e.g. a static-only invocation) is treated as "not verified running", not as a free pass.
    /// Returns the (possibly capped) score and a human-readable reason when a gate failed.
    /// </summary>
    public static (double? score, string? reason) CapForExecutability(
        double? score, bool? builds, bool? boots, bool deep = false, bool hasRunnableSystem = true)
    {
        if (builds == false)
            return (CapTo(score, BuildFailCap),
                $"capped at {BuildFailCap:0.0}/5 — source does not compile (dotnet build failed)");

        // In a deep evaluation the system MUST run and be verified live. Anything short of an observed
        // healthy boot cannot be trusted as production-grade, so it is capped hard.
        if (deep && boots != true)
        {
            if (!hasRunnableSystem)
                return (CapTo(score, NoRunnableSystemCap),
                    $"capped at {NoRunnableSystemCap:0.0}/5 — no docker-compose.yml: the submission delivers no runnable system (never exercised)");
            return (CapTo(score, BootFailCap),
                $"capped at {BootFailCap:0.0}/5 — the system was never verified running (/health never returned 2xx); a deep score requires a live boot");
        }
        return (score, null);
    }

    private static double CapTo(double? score, double cap) => score is double v ? Math.Min(v, cap) : cap;
}
