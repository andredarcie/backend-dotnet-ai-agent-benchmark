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
        AutomationLevel.FullAuto => "full-auto",
        AutomationLevel.SemiOracle => "semi (oracle 1x)",
        AutomationLevel.ProxyReview => "proxy + review",
        _ => "?",
    };
}
