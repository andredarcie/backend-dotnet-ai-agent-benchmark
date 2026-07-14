using BackendEvaluator.Core;
using Xunit;

namespace Evaluator.Tests;

public class ScoringTests
{
    private static CategoryResult Cat(int n, double weight, params MetricStatus[] metrics)
    {
        var c = new CategoryResult { Number = n, Name = $"cat{n}", Weight = weight, Automation = AutomationLevel.FullAuto };
        int i = 0;
        foreach (var s in metrics)
            c.Metrics.Add(new MetricResult { Name = $"m{i++}", Status = s });
        return c;
    }

    [Fact]
    public void CategoryScore_is_weighted_mean_of_measured_metrics_times_five()
    {
        // one Pass (1.0) + one Fail (0.0) -> quality 0.5 -> 2.5 / 5.
        Assert.Equal(2.5, Cat(1, 0.1, MetricStatus.Pass, MetricStatus.Fail).Score);
    }

    [Fact]
    public void CategoryScore_excludes_indeterminate_metrics()
    {
        // Indeterminate is dropped, so two Pass -> full score, not diluted.
        Assert.Equal(5.0, Cat(1, 0.1, MetricStatus.Pass, MetricStatus.Pass, MetricStatus.Indeterminate).Score);
    }

    [Fact]
    public void CategoryScore_is_null_when_nothing_measured()
    {
        Assert.Null(Cat(1, 0.1, MetricStatus.Indeterminate, MetricStatus.Indeterminate).Score);
    }

    [Fact]
    public void Aggregate_renormalizes_over_scored_categories_and_reports_coverage()
    {
        var cats = new[]
        {
            Cat(1, 0.12, MetricStatus.Pass),            // score 5.0
            Cat(2, 0.08, MetricStatus.Fail),            // score 0.0
            Cat(3, 0.10, MetricStatus.Indeterminate),   // unscored -> excluded from the weighted mean
        };

        var (weighted, coverage) = Scoring.Aggregate(cats);

        // (5.0*0.12 + 0.0*0.08) / (0.12 + 0.08) = 3.0, renormalized over the two scored categories.
        Assert.Equal(3.0, weighted);
        Assert.Equal(2.0 / 3.0, coverage, 3);
    }

    [Fact]
    public void Aggregate_excludes_informational_categories_from_the_score_and_from_coverage()
    {
        // A weight-0 category is INFORMATIONAL: measured and printed, but it must not move the weighted
        // score, and it must not count against coverage either (it is not a category we failed to measure).
        var cats = new[]
        {
            Cat(1, 0.20, MetricStatus.Pass),   // score 5.0
            Cat(2, 0.20, MetricStatus.Fail),   // score 0.0
            Cat(9, 0.00, MetricStatus.Fail),   // informational: a total failure here changes nothing
        };

        var (weighted, coverage) = Scoring.Aggregate(cats);

        Assert.Equal(2.5, weighted);      // (5.0*0.20 + 0.0*0.20) / 0.40 — the informational Fail is ignored
        Assert.Equal(1.0, coverage, 3);   // both RANKED categories were measured => full coverage
    }

    [Fact]
    public void Aggregate_returns_null_when_only_informational_categories_exist()
    {
        var (weighted, coverage) = Scoring.Aggregate(new[] { Cat(9, 0.00, MetricStatus.Pass) });
        Assert.Null(weighted);
        Assert.Equal(0.0, coverage);
    }

    [Fact]
    public void Registry_has_eight_scored_categories_whose_weights_sum_to_one()
    {
        var all = BackendEvaluator.Evaluators.EvaluatorRegistry.All;
        var scored = all.Where(c => c.Weight > 0).ToList();

        Assert.Equal(8, scored.Count);
        Assert.Equal(1.00, scored.Sum(c => c.Weight), 3);
        Assert.All(all.Where(c => c.Weight <= 0), c => Assert.Contains("informational", c.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Aggregate_returns_null_score_when_no_category_scored()
    {
        var (weighted, coverage) = Scoring.Aggregate(new[] { Cat(1, 0.1, MetricStatus.Indeterminate) });
        Assert.Null(weighted);
        Assert.Equal(0.0, coverage);
    }

    [Fact]
    public void Aggregate_handles_an_empty_report()
    {
        var (weighted, coverage) = Scoring.Aggregate(System.Array.Empty<CategoryResult>());
        Assert.Null(weighted);
        Assert.Equal(0.0, coverage);
    }

    [Fact]
    public void CapForExecutability_caps_at_0_5_when_build_fails()
    {
        // build failure is the gravest cap (0.5) and applies regardless of mode: whoever doesn't even
        // compile is punished the most.
        var (s, reason) = Scoring.CapForExecutability(4.8, builds: false, boots: true, deep: true, hasRunnableSystem: true);
        Assert.Equal(0.5, s);
        Assert.NotNull(reason);
    }

    [Fact]
    public void CapForExecutability_caps_at_1_5_when_deep_boot_fails()
    {
        // has a runnable system but /health never came up ⇒ grave boot cap (1.5)
        var (s, reason) = Scoring.CapForExecutability(4.8, builds: true, boots: false, deep: true, hasRunnableSystem: true);
        Assert.Equal(1.5, s);
        Assert.NotNull(reason);
    }

    [Fact]
    public void CapForExecutability_caps_a_deep_run_that_was_never_booted_even_if_boots_unknown()
    {
        // THE RUN IS THE MEASUREMENT: a deep run that never observed a healthy boot (boots == null, e.g.
        // a static-only invocation) is NOT a free pass — it is capped exactly like an outright boot failure.
        var (s, reason) = Scoring.CapForExecutability(4.9, builds: true, boots: null, deep: true, hasRunnableSystem: true);
        Assert.Equal(1.5, s);
        Assert.NotNull(reason);
    }

    [Fact]
    public void CapForExecutability_caps_at_1_when_no_runnable_system()
    {
        // no docker-compose.yml at all ⇒ no runnable system delivered ⇒ capped at 1.0 (worse than a
        // boot-fail, but not as grave as source that doesn't even compile)
        var (s, reason) = Scoring.CapForExecutability(4.27, builds: true, boots: null, deep: true, hasRunnableSystem: false);
        Assert.Equal(1.0, s);
        Assert.NotNull(reason);
    }

    [Fact]
    public void CapForExecutability_build_failure_takes_precedence_over_boot()
    {
        var (s, _) = Scoring.CapForExecutability(4.8, builds: false, boots: false, deep: true, hasRunnableSystem: true);
        Assert.Equal(0.5, s);
    }

    [Fact]
    public void CapForExecutability_never_raises_an_already_lower_score_but_still_flags()
    {
        var (s, reason) = Scoring.CapForExecutability(0.3, builds: false, boots: true, deep: true, hasRunnableSystem: true);
        Assert.Equal(0.3, s);
        Assert.NotNull(reason);
    }

    [Fact]
    public void CapForExecutability_no_cap_when_deep_and_verified_running()
    {
        // the only way to earn an uncapped deep score: actually booted and observed healthy
        Assert.Equal((4.8, (string?)null), Scoring.CapForExecutability(4.8, true, true, deep: true, hasRunnableSystem: true));
    }

    [Fact]
    public void CapForExecutability_light_mode_is_not_boot_gated()
    {
        // light (non-deep) mode is static-only by design and excluded from the ranked leaderboard, so it
        // is not gated on boot even though builds/boots are unknown.
        Assert.Equal((4.8, (string?)null), Scoring.CapForExecutability(4.8, null, null, deep: false, hasRunnableSystem: true));
    }
}
