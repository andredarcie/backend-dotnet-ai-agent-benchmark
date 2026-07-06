namespace BackendEvaluator.Core;

/// <summary>Everything an evaluator needs: the inspected project, the tool runner and the run options.</summary>
public sealed class EvaluationContext
{
    public required ProjectInspector Project { get; init; }
    public required CodeFacts Facts { get; init; }
    public required ToolRunner Tools { get; init; }
    public required EvaluatorOptions Options { get; init; }
    public Action<string> Log { get; init; } = _ => { };

    /// <summary>Result of driving the live API contract (set once when a base URL is given); null otherwise.</summary>
    public ContractReport? Contract { get; init; }

    /// <summary>Resolved .sln/.slnx the deep dotnet commands should target (set once by Runner); null when the
    /// target has none, in which case those commands run with no project/solution argument in <see cref="Project"/>.Root.</summary>
    public string? SolutionPath { get; set; }

    /// <summary>Warning count parsed from Runner's single Release build gate summary; null when the gate did not
    /// run (light mode or dotnet missing) or its summary could not be parsed.</summary>
    public int? BuildWarnings { get; set; }

    // Memoizes the single shared `dotnet test` run so Functional (#1) and Tests (#9) don't each run the
    // submission's suite (two full runs that can even disagree). Collected with coverage so the one run
    // serves both the pass-rate parse (Functional) and the cobertura coverage read (Tests).
    private ToolOutcome? _dotnetTest;
    private bool _dotnetTestRan;

    /// <summary>
    /// Runs `dotnet test` (with XPlat Code Coverage, targeting <see cref="SolutionPath"/> when set) exactly
    /// once and caches the outcome; subsequent calls return the cached result. Returns null when the
    /// `dotnet` tool is unavailable.
    /// </summary>
    public ToolOutcome? RunDotnetTestOnce(int timeoutMs = 600_000)
    {
        if (_dotnetTestRan) return _dotnetTest;
        _dotnetTestRan = true;
        if (!Tools.IsAvailable("dotnet")) return _dotnetTest = null;

        // Delete any PRE-EXISTING coverage reports (stale from an earlier run, or committed into the
        // submission) before testing, so the coverage merge in TestsEvaluator counts ONLY reports this run
        // produces. Otherwise a leftover — or a hand-committed cobertura.xml — would inflate or outright game
        // the coverage figure.
        try
        {
            foreach (var stale in Directory.EnumerateFiles(Project.Root, "coverage.cobertura.xml", SearchOption.AllDirectories))
                try { File.Delete(stale); } catch { /* best-effort */ }
        }
        catch { /* best-effort */ }

        Log("    running dotnet test (shared) ...");
        string args = SolutionPath != null
            ? $"test \"{SolutionPath}\" --nologo --collect:\"XPlat Code Coverage\" --verbosity quiet"
            : "test --nologo --collect:\"XPlat Code Coverage\" --verbosity quiet";
        return _dotnetTest = Tools.Run("dotnet", args, Project.Root, timeoutMs);
    }
}
