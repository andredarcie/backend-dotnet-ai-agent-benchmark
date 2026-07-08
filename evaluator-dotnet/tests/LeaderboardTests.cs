using BackendEvaluator.Core;
using Xunit;

namespace Evaluator.Tests;

public class LeaderboardTests
{
    [Fact]
    public void Median_handles_odd_and_even_counts()
    {
        Assert.Equal(3.0, Leaderboard.Median(new[] { 1.0, 3.0, 5.0 }));
        Assert.Equal(3.0, Leaderboard.Median(new[] { 2.0, 4.0 }));
        Assert.Equal(0.0, Leaderboard.Median(System.Array.Empty<double>()));
    }

    [Fact]
    public void StdDev_is_zero_for_a_single_run()
    {
        Assert.Equal(0.0, Leaderboard.StdDev(new[] { 4.7 }));
        Assert.True(Leaderboard.StdDev(new[] { 1.0, 5.0 }) > 0);
    }

    [Fact]
    public void Aggregate_ranks_by_median_and_flags_provisional()
    {
        var runs = new[]
        {
            new RunScore("model-a", "run1", 3.0),
            new RunScore("model-a", "run2", 5.0),   // model-a median = 4.0
            new RunScore("model-b", "run1", 4.5),   // model-b median = 4.5 (single run -> provisional)
        };

        var rows = Leaderboard.Aggregate(runs);

        Assert.Equal("model-b", rows[0].Model);     // 4.5 > 4.0
        Assert.Equal(4.0, rows[1].Median);
        Assert.True(rows[0].Provisional);           // 1 run
        Assert.True(rows[1].Provisional);           // 2 runs (< 5)
        Assert.Equal(2, rows[1].Count);
    }

    [Fact]
    public void Aggregate_marks_baseline_models()
    {
        var rows = Leaderboard.Aggregate(new[] { new RunScore("_baseline/reference", "run1", 5.0) });
        Assert.True(rows[0].IsBaseline);
    }
}
