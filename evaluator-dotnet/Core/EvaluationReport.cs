namespace BackendEvaluator.Core;

/// <summary>The full evaluation result for one target, ready to serialize.</summary>
public sealed class EvaluationReport
{
    public required string Target { get; init; }
    public required string EvaluatedAtUtc { get; init; }
    public bool Deep { get; init; }
    public List<string> Environment { get; } = new();
    public List<CategoryResult> Categories { get; } = new();

    /// <summary>0..5, renormalized over the categories that produced a score (after any executability cap).</summary>
    public double? WeightedScore { get; set; }

    /// <summary>Fraction of categories that produced a score.</summary>
    public double Coverage { get; set; }

    /// <summary>Whether the source compiles (`dotnet build`). Null when not checked (light mode).</summary>
    public bool? Builds { get; set; }

    /// <summary>Whether the service became healthy (`/health` 2xx). Null when not checked (no base URL).</summary>
    public bool? Boots { get; set; }

    /// <summary>Set when the score was capped because the submission doesn't build/boot.</summary>
    public string? ScoreCapReason { get; set; }

    /// <summary>Penalty (0..5 points) subtracted because the run needed a minimal build/boot patch.</summary>
    public double? PatchPenalty { get; set; }

    /// <summary>Why the run was patched (from the submission's bench-patch.json marker).</summary>
    public string? PatchReason { get; set; }
}
