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

    // A submission that doesn't compile or boot cannot be "production-grade" however clean its source
    // reads — so quality alone must not earn a high score. These caps make the headline honest.
    public const double BuildFailCap = 1.0;
    public const double BootFailCap = 2.5;

    /// <summary>
    /// Caps the weighted score when the submission fails the executability gate: a build failure caps at
    /// <see cref="BuildFailCap"/>, a boot failure at <see cref="BootFailCap"/>. Returns the (possibly
    /// capped) score and a human-readable reason when a gate failed (even if no cap was needed).
    /// </summary>
    public static (double? score, string? reason) CapForExecutability(double? score, bool? builds, bool? boots)
    {
        if (builds == false)
        {
            double capped = score is double b ? Math.Min(b, BuildFailCap) : BuildFailCap;
            return (capped, $"capped at {BuildFailCap:0.0}/5 — source does not compile (dotnet build failed)");
        }
        if (boots == false)
        {
            double capped = score is double k ? Math.Min(k, BootFailCap) : BootFailCap;
            return (capped, $"capped at {BootFailCap:0.0}/5 — service did not become healthy (/health never returned 2xx)");
        }
        return (score, null);
    }

    /// <summary>
    /// Subtracts a patch penalty (a run minimally patched to build/boot is graded on its merits, then
    /// docked) and clamps to [0, 5]. The penalty is recorded by the submission's bench-patch.json marker.
    /// </summary>
    public static double? ApplyPatchPenalty(double? score, double penaltyPoints)
        => score is double s ? Math.Clamp(Math.Round(s - penaltyPoints, 2), 0, 5) : score;
}
