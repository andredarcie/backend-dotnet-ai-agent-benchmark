namespace BackendEvaluator.Core;

/// <summary>
/// How directly a category is measured. Every level is scored 100% by the machine — no human is ever
/// in the loop; the badge only communicates how direct the measurement is. Mirrors the 🟢/🟡/🟠 badges
/// in EVALUATION-CRITERIA.md.
/// </summary>
public enum AutomationLevel
{
    /// <summary>🟢 Deterministic — scored 100% by machine from static analysis; same source ⇒ same score.</summary>
    FullAuto,

    /// <summary>🟡 Oracle-based — scored 100% by machine each run against a fixed oracle/threshold defined once (acceptance suite, expected status codes, SLO).</summary>
    SemiOracle,

    /// <summary>🟠 Proxy — scored 100% by machine from an objective proxy metric (coupling, rule-violation counts, presence checks). Less direct than a deterministic count, but still fully automated.</summary>
    ProxyReview,
}
