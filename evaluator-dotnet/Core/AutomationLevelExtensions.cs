namespace BackendEvaluator.Core;

public static class AutomationLevelExtensions
{
    public static string Badge(this AutomationLevel a) => a switch
    {
        AutomationLevel.FullAuto => "🟢",
        AutomationLevel.SemiOracle => "🟡",
        AutomationLevel.ProxyReview => "🟠",
        _ => "?",
    };

    public static string Label(this AutomationLevel a) => a switch
    {
        AutomationLevel.FullAuto => "deterministic",
        AutomationLevel.SemiOracle => "oracle",
        AutomationLevel.ProxyReview => "proxy",
        _ => "?",
    };
}
