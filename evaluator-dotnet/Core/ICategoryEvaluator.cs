namespace BackendEvaluator.Core;

/// <summary>Contract for an evaluator of one of the 13 categories.</summary>
public interface ICategoryEvaluator
{
    int Number { get; }
    string Name { get; }
    double Weight { get; }
    AutomationLevel Automation { get; }
    Task<CategoryResult> EvaluateAsync(EvaluationContext ctx);
}
