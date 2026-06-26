namespace BackendEvaluator.Core;

/// <summary>One scored run of a model (e.g. model "claude-haiku-4-5", run "run3").</summary>
public sealed record RunScore(string Model, string Run, double Score);

/// <summary>Per-model aggregate over its runs: median is the rank key; spread shows the noise.</summary>
public sealed record ModelStats(
    string Model, double Median, double Mean, double StdDev,
    double Min, double Max, int Count, bool Provisional, IReadOnlyList<string> Runs)
{
    /// <summary>A reference/known-good anchor, shown apart from the ranking rather than competing in it.</summary>
    public bool IsBaseline => Leaderboard.IsBaselineModel(Model);
}

/// <summary>
/// Aggregates many runs per model into a ranking. Models are stochastic, so a single run is a weak
/// sample: rank by the <b>median</b> across runs and report the spread (±σ, range, count). Models with
/// fewer than <see cref="ProvisionalThreshold"/> runs are flagged provisional.
/// </summary>
public static class Leaderboard
{
    public const int ProvisionalThreshold = 5;

    public static bool IsBaselineModel(string model)
        => model.StartsWith("_baseline", StringComparison.OrdinalIgnoreCase)
           || model.StartsWith("baseline", StringComparison.OrdinalIgnoreCase)
           || model.StartsWith("_reference", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<ModelStats> Aggregate(IEnumerable<RunScore> runs, int provisionalThreshold = ProvisionalThreshold)
        => runs
            .GroupBy(r => r.Model)
            .Select(g =>
            {
                var sorted = g.Select(r => r.Score).OrderBy(x => x).ToList();
                return new ModelStats(
                    g.Key, Median(sorted), Mean(sorted), StdDev(sorted),
                    sorted[0], sorted[^1], sorted.Count,
                    sorted.Count < provisionalThreshold,
                    g.OrderBy(r => r.Run, StringComparer.OrdinalIgnoreCase).Select(r => r.Run).ToList());
            })
            .OrderByDescending(s => s.Median)
            .ThenByDescending(s => s.Mean)
            .ToList();

    /// <summary>Median of an ascending-sorted, non-empty list.</summary>
    public static double Median(IReadOnlyList<double> sorted)
    {
        int n = sorted.Count;
        if (n == 0) return 0;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
    }

    public static double Mean(IReadOnlyList<double> xs) => xs.Count == 0 ? 0 : xs.Average();

    /// <summary>Population standard deviation (0 for a single run).</summary>
    public static double StdDev(IReadOnlyList<double> xs)
    {
        if (xs.Count < 2) return 0;
        double m = xs.Average();
        return Math.Sqrt(xs.Sum(x => (x - m) * (x - m)) / xs.Count);
    }
}
