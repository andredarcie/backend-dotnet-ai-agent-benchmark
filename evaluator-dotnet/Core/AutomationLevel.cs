namespace BackendEvaluator.Core;

/// <summary>
/// Mirrors the 🟢/🟡/🟠 badges in EVALUATION-CRITERIA.md.
/// </summary>
public enum AutomationLevel
{
    /// <summary>🟢 Score produced 100% by machine.</summary>
    FullAuto,

    /// <summary>🟡 Automatic each run AFTER a one-time oracle/threshold is defined.</summary>
    SemiOracle,

    /// <summary>🟠 Machine measures proxies; the final verdict needs a human.</summary>
    ProxyReview,
}
