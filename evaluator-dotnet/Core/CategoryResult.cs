namespace BackendEvaluator.Core;

/// <summary>The outcome of evaluating one of the 13 categories.</summary>
public sealed class CategoryResult
{
    public required int Number { get; init; }
    public required string Name { get; init; }
    public required double Weight { get; init; } // 0..1
    public required AutomationLevel Automation { get; init; }
    public List<MetricResult> Metrics { get; } = new();
    public List<string> Notes { get; } = new();
    public List<string> MissingTools { get; } = new();

    public string Badge => Automation.Badge();

    /// <summary>0..5, or null when nothing could be measured (all metrics indeterminate / no tools).</summary>
    public double? Score
    {
        get
        {
            var measured = Metrics.Where(m => m.Status != MetricStatus.Indeterminate).ToList();
            if (measured.Count == 0) return null;
            double wsum = measured.Sum(m => m.Weight);
            if (wsum <= 0) return null;
            double q = measured.Sum(m => m.Quality * m.Weight) / wsum;
            return Math.Round(q * 5.0, 1);
        }
    }

    public int MeasuredCount => Metrics.Count(m => m.Status != MetricStatus.Indeterminate);
    public int IndeterminateCount => Metrics.Count(m => m.Status == MetricStatus.Indeterminate);
}
