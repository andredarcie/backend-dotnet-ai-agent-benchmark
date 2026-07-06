namespace BackendEvaluator.Core;

/// <summary>Base class with shared metric-building helpers for the category evaluators.</summary>
public abstract class CategoryEvaluatorBase : ICategoryEvaluator
{
    public abstract int Number { get; }
    public abstract string Name { get; }
    public abstract double Weight { get; }
    public abstract AutomationLevel Automation { get; }
    public abstract Task<CategoryResult> EvaluateAsync(EvaluationContext ctx);

    protected CategoryResult New() => new()
    {
        Number = Number, Name = Name, Weight = Weight, Automation = Automation,
    };

    protected static MetricResult Pass(string name, string observed, string target, string? note = null, double weight = 1)
        => new() { Name = name, Observed = observed, Target = target, Status = MetricStatus.Pass, Note = note, Weight = weight };

    protected static MetricResult Fail(string name, string observed, string target, string? note = null, double weight = 1)
        => new() { Name = name, Observed = observed, Target = target, Status = MetricStatus.Fail, Note = note, Weight = weight };

    protected static MetricResult Partial(string name, string observed, string target, string? note = null, double weight = 1)
        => new() { Name = name, Observed = observed, Target = target, Status = MetricStatus.Partial, Note = note, Weight = weight };

    protected static MetricResult Unknown(string name, string target, string? note = null, double weight = 1)
        => new() { Name = name, Observed = "indeterminate", Target = target, Status = MetricStatus.Indeterminate, Note = note, Weight = weight };

    protected static MetricResult Bool(string name, bool ok, string target, string? note = null, double weight = 1)
        => ok ? Pass(name, "yes", target, note, weight) : Fail(name, "no", target, note, weight);

    /// <summary>Maps a quality value in [0,1] to Pass (>=1) / Partial (>=0.5) / Fail.</summary>
    protected static MetricResult Grade(string name, double q, string observed, string target, string? note = null, double weight = 1)
        => q >= 0.999 ? Pass(name, observed, target, note, weight)
         : q >= 0.5 ? Partial(name, observed, target, note, weight)
         : Fail(name, observed, target, note, weight);

    /// <summary>
    /// True when a tool ran but its output shows it could not actually perform its check — a schema-load
    /// error, a runtime/interpreter crash, an unrecognized-flag error, an empty/uncollected test suite —
    /// as opposed to legitimately reporting a violation. Such a result MUST become Indeterminate (excluded
    /// from the score), never a Fail/Partial: a broken or misconfigured tool is not a defect of the
    /// submission. Callers pass the substrings that identify "the tool never really ran" for that tool.
    /// </summary>
    protected static bool CouldNotRun(ToolOutcome o, params string[] signatures)
        => signatures.Any(s => o.Combined.Contains(s, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Runs a local support tool when it is installed and turns the result into a metric; when the
    /// tool is missing, records an Indeterminate metric (excluded from the score) plus an install hint,
    /// so the report shows the tool is wired but not present. Returns whether the tool ran.
    /// </summary>
    protected static bool RunTool(
        EvaluationContext ctx, CategoryResult r, string tool, string args, string metricName, string target,
        Func<ToolOutcome, MetricResult> onResult, double weight = 1, int timeoutMs = 180_000)
    {
        if (!ctx.Tools.IsAvailable(tool))
        {
            r.MissingTools.Add(tool);
            r.Metrics.Add(Unknown(metricName, target, $"tool '{tool}' not installed — install: {ToolCatalog.Install(tool)}", weight));
            return false;
        }
        ctx.Log($"    running {tool} ...");
        var outcome = ctx.Tools.Run(tool, args, ctx.Project.Root, timeoutMs);
        if (outcome.NotFound)
        {
            r.MissingTools.Add(tool);
            r.Metrics.Add(Unknown(metricName, target, $"tool '{tool}' could not be launched", weight));
            return false;
        }
        if (outcome.TimedOut)
        {
            r.Metrics.Add(Unknown(metricName, target, $"tool '{tool}' timed out", weight));
            return false;
        }
        r.Metrics.Add(onResult(outcome));
        return true;
    }
}
