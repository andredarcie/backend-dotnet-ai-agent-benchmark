namespace BackendEvaluator.Core;

/// <summary>The result of invoking an external command-line tool.</summary>
public sealed record ToolOutcome(bool Ran, bool NotFound, int ExitCode, string Stdout, string Stderr, bool TimedOut)
{
    public bool Success => Ran && !TimedOut && ExitCode == 0;
    public string Combined => (Stdout + "\n" + Stderr).Trim();
}
