using System.Text.Json.Serialization;

namespace BackendEvaluator.Core;

/// <summary>A single measured signal within a category.</summary>
public sealed class MetricResult
{
    public required string Name { get; init; }
    public string Observed { get; init; } = "";
    public string Target { get; init; } = "";
    public MetricStatus Status { get; init; } = MetricStatus.Indeterminate;
    public double Weight { get; init; } = 1.0;
    public string? Note { get; init; }

    /// <summary>Quality in [0,1]; NaN for indeterminate (excluded from scoring). Not serialized (NaN is invalid JSON).</summary>
    [JsonIgnore]
    public double Quality => Status switch
    {
        MetricStatus.Pass => 1.0,
        MetricStatus.Partial => 0.5,
        MetricStatus.Fail => 0.0,
        _ => double.NaN,
    };
}
