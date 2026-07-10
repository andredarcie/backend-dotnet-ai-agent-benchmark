using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ModelRunner;

/// <summary>
/// Generates ONE benchmark run for a model and stamps its provenance sidecar.
///
/// You say WHICH model; this does the rest, cross-platform (Windows/macOS/Linux):
///   1. picks the next runN under submissions/&lt;model&gt;/
///   2. PASS 1 — feeds PROMPT.md to the model's CLI; the model writes the whole project
///      into submissions/&lt;model&gt;/&lt;runN&gt;/
///   3. PASS 2 — feeds PROMPT.md + PROMPT-REVIEW.md back in the SAME folder so the model
///      reviews, runs, validates and applies a final patch ("second chance")
///   4. times both passes, captures token/cost usage when the CLI reports it, and writes
///      submissions/&lt;model&gt;/&lt;runN&gt;.meta.json — the provenance the site shows.
///
/// The meta.json is authored provenance about HOW the run was produced; the evaluator never
/// reads it (it lives outside the graded tree). See submissions/README.md.
/// </summary>
internal static class Program
{
    // ── MODEL MATRIX ── the only thing to confirm/edit. key = submission folder == leaderboard label.
    //   ⚠ Verify each `Model` id against the tool's /model picker; a wrong id fails with "unknown model".
    private static readonly Dictionary<string, ModelSpec> Matrix = new(StringComparer.OrdinalIgnoreCase)
    {
        ["opus-4-8"]  = new("claude", "claude-opus-4-8",           "high", "Claude Code"),
        ["sonnet-5"]  = new("claude", "claude-sonnet-5",           "high", "Claude Code"),
        ["haiku-4-5"] = new("claude", "claude-haiku-4-5-20251001", "",     "Claude Code"),
        ["fable-5"]   = new("claude", "claude-fable-5",            "high", "Claude Code"),
        ["gpt-5-5"]   = new("codex",  "gpt-5.5-codex",             "high", "Codex CLI"),
        ["gemini"]    = new("agy",    "gemini-3-pro",              "",     "Antigravity (agy)"), // ⚠ confirm id
    };

    private static int Main(string[] args)
    {
        try { return Run(args); }
        catch (UsageException ux) { Console.Error.WriteLine("error: " + ux.Message); return 2; }
        catch (Exception ex) { Console.Error.WriteLine("error: " + ex.Message); return 1; }
    }

    private static int Run(string[] args)
    {
        // ---- parse args -----------------------------------------------------
        string? model = null, effortOverride = null, repoOverride = null;
        int forcedRun = 0;
        bool dryRun = false, noReview = false, hasEffort = false;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            switch (a)
            {
                case "-h": case "--help": PrintHelp(); return 0;
                case "--list": PrintList(); return 0;
                case "--dry-run": dryRun = true; break;
                case "--no-review": noReview = true; break;
                case "--effort": effortOverride = Next(args, ref i, a); hasEffort = true; break;
                case "--run": forcedRun = int.Parse(Next(args, ref i, a)); break;
                case "--repo": repoOverride = Next(args, ref i, a); break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option '{a}' (try --help)");
                    if (model != null) throw new UsageException($"unexpected argument '{a}'");
                    model = a;
                    break;
            }
        }
        if (model == null) { PrintHelp(); return 2; }
        if (!Matrix.TryGetValue(model, out var spec))
            throw new UsageException($"unknown model '{model}'. Known: {string.Join(", ", Matrix.Keys)}");

        string repo = repoOverride != null ? Path.GetFullPath(repoOverride) : FindRepoRoot(Directory.GetCurrentDirectory());
        string promptFile = Path.Combine(repo, "PROMPT.md");
        string reviewFile = Path.Combine(repo, "PROMPT-REVIEW.md");
        if (!File.Exists(promptFile)) throw new UsageException($"PROMPT.md not found under {repo} (pass --repo)");
        if (!noReview && !File.Exists(reviewFile)) throw new UsageException($"PROMPT-REVIEW.md not found under {repo} (or pass --no-review)");

        string effort = hasEffort ? (effortOverride ?? "") : spec.Effort;
        string modelDir = Path.Combine(repo, "submissions", model);
        int runNo = forcedRun > 0 ? forcedRun : NextRunNumber(modelDir);
        string runName = $"run{runNo}";
        string runDir = Path.Combine(modelDir, runName);
        string metaOut = Path.Combine(modelDir, $"{runName}.meta.json");
        int passes = noReview ? 1 : 2;

        Console.WriteLine();
        Console.WriteLine($"==> {model}  {runName}   cli={spec.Cli}  model={spec.Model}  " +
                          $"effort={(effort.Length > 0 ? effort : "(none)")}  passes={passes}");
        Console.WriteLine($"    output : {runDir}");
        Console.WriteLine($"    meta   : {metaOut}");
        if (dryRun) { Console.WriteLine("    (dry run — nothing executed)"); return 0; }

        if (Directory.Exists(runDir))
            throw new UsageException($"run dir already exists: {runDir} (pass --run N to choose another number)");
        if (ResolveExecutable(spec.Cli) == null)
            throw new UsageException($"CLI '{spec.Cli}' not found on PATH");

        Directory.CreateDirectory(runDir);
        string harnessVersion = SafeVersion(spec.Cli);
        string promptVersion = PromptVersion(repo, noReview);

        string prompt = File.ReadAllText(promptFile);
        string pass2Input = noReview ? "" : prompt + "\n\n---\n\n" + File.ReadAllText(reviewFile);

        // ---- run the passes, timed, accumulating usage ----------------------
        long? tokensIn = null, tokensOut = null;
        double? costUsd = null;
        var sw = Stopwatch.StartNew();

        Console.WriteLine($"\n--- pass 1/{passes}: build (PROMPT.md) ---");
        var u1 = RunAgent(spec, effort, runDir, repo, prompt);
        Accumulate(ref tokensIn, ref tokensOut, ref costUsd, u1);
        if (u1.Exit != 0) Console.Error.WriteLine($"warning: {spec.Cli} pass 1 exited with code {u1.Exit}");

        if (!noReview)
        {
            Console.WriteLine($"\n--- pass 2/{passes}: review + validate + patch (PROMPT-REVIEW.md) ---");
            var u2 = RunAgent(spec, effort, runDir, repo, pass2Input);
            Accumulate(ref tokensIn, ref tokensOut, ref costUsd, u2);
            if (u2.Exit != 0) Console.Error.WriteLine($"warning: {spec.Cli} pass 2 exited with code {u2.Exit}");
        }

        sw.Stop();
        int durationSec = (int)Math.Round(sw.Elapsed.TotalSeconds);
        int fileCount = Directory.Exists(runDir)
            ? Directory.EnumerateFiles(runDir, "*", SearchOption.AllDirectories).Count() : 0;
        if (fileCount == 0) Console.Error.WriteLine($"warning: no files were written into {runDir}");

        // ---- write the provenance sidecar -----------------------------------
        WriteMeta(metaOut, spec.Harness, harnessVersion, effort, durationSec, passes,
                  tokensIn, tokensOut, costUsd, promptVersion);

        Console.WriteLine();
        Console.WriteLine($"OK — {model}/{runName}: {fileCount} file(s), {passes} pass(es), " +
                          $"{TimeSpan.FromSeconds(durationSec):hh\\:mm\\:ss}.");
        Console.WriteLine($"     meta : {metaOut}");
        if (tokensIn == null && costUsd == null)
            Console.WriteLine("     note : tokens/cost not captured for this CLI — fill them into the meta manually.");
        Console.WriteLine();
        Console.WriteLine("Next: grade it deep, then rebuild the site data:");
        Console.WriteLine($"     dotnet run --project evaluator-dotnet -- {model}/{runName} --deep");
        Console.WriteLine( "     ./docs/generate-data.ps1");
        return 0;
    }

    // ── agent invocation ────────────────────────────────────────────────────

    /// <summary>Runs one pass. Returns exit code + parsed usage (usage only for claude JSON output).</summary>
    private static Usage RunAgent(ModelSpec spec, string effort, string runDir, string repo, string input)
    {
        bool hasEffort = effort.Length > 0;
        switch (spec.Cli)
        {
            case "claude":
            {
                // print mode + JSON envelope so cost/usage can be read back; prompt via stdin.
                var a = new List<string> { "-p", "--output-format", "json", "--model", spec.Model };
                if (hasEffort) { a.Add("--effort"); a.Add(effort); }
                a.Add("--dangerously-skip-permissions");
                int exit = RunCaptured(spec.Cli, a, runDir, input, out string stdout);
                var (ti, to, cost) = ReadClaudeUsage(stdout);
                return new Usage(exit, ti, to, cost);
            }
            case "codex":
            {
                var a = new List<string> { "exec", "-m", spec.Model };
                if (hasEffort) { a.Add("-c"); a.Add($"model_reasoning_effort=\"{effort}\""); }
                a.AddRange(new[] { "-C", runDir, "--skip-git-repo-check", "--dangerously-bypass-approvals-and-sandbox", "-" });
                int exit = RunCaptured(spec.Cli, a, repo, input, out _);
                return new Usage(exit, null, null, null); // codex usage format is unstable; leave blank
            }
            case "agy":
            {
                // ⚠ agy print mode only works from a REAL interactive terminal and its output can't be
                //   captured (upstream TTY bug). Inherit the console; pass the prompt as an argument.
                EnsureAgyTrusted(runDir);
                var a = new List<string> { "-p", input, "--model", spec.Model,
                                           "--dangerously-skip-permissions", "--print-timeout", "30m" };
                int exit = RunInherited(spec.Cli, a, runDir);
                return new Usage(exit, null, null, null);
            }
            default: throw new UsageException($"unknown cli '{spec.Cli}'");
        }
    }

    private static void Accumulate(ref long? ti, ref long? to, ref double? cost, Usage u)
    {
        if (u.TokensIn is long a) ti = (ti ?? 0) + a;
        if (u.TokensOut is long b) to = (to ?? 0) + b;
        if (u.CostUsd is double c) cost = (cost ?? 0) + c;
    }

    // ── process helpers (cross-platform) ────────────────────────────────────

    /// <summary>Runs a child process capturing stdout (stderr inherited); optionally writes stdin.</summary>
    private static int RunCaptured(string exe, IList<string> cliArgs, string? workDir, string? stdin, out string stdout)
    {
        var psi = BuildStartInfo(exe, cliArgs, workDir);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardInput = stdin != null;
        using var p = Process.Start(psi) ?? throw new Exception($"failed to start {exe}");
        var outTask = p.StandardOutput.ReadToEndAsync();
        if (stdin != null) { p.StandardInput.Write(stdin); p.StandardInput.Close(); }
        p.WaitForExit();
        stdout = outTask.GetAwaiter().GetResult();
        return p.ExitCode;
    }

    /// <summary>Runs a child process inheriting the console (needed for agy's TTY requirement).</summary>
    private static int RunInherited(string exe, IList<string> cliArgs, string? workDir)
    {
        var psi = BuildStartInfo(exe, cliArgs, workDir);
        using var p = Process.Start(psi) ?? throw new Exception($"failed to start {exe}");
        p.WaitForExit();
        return p.ExitCode;
    }

    private static ProcessStartInfo BuildStartInfo(string exe, IEnumerable<string> cliArgs, string? workDir)
    {
        string resolved = ResolveExecutable(exe) ?? throw new UsageException($"CLI '{exe}' not found on PATH");
        var psi = new ProcessStartInfo { UseShellExecute = false };
        if (workDir != null) psi.WorkingDirectory = workDir;

        // On Windows, npm-installed CLIs are .cmd/.bat shims that can't be launched directly — go via cmd.exe.
        if (OperatingSystem.IsWindows() &&
            (resolved.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
             resolved.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)))
        {
            psi.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(resolved);
        }
        else psi.FileName = resolved;

        foreach (var arg in cliArgs) psi.ArgumentList.Add(arg);
        return psi;
    }

    /// <summary>Finds an executable on PATH, honoring PATHEXT (.cmd/.exe/…) on Windows.</summary>
    private static string? ResolveExecutable(string name)
    {
        if (name.Contains('/') || name.Contains('\\'))
            return File.Exists(name) ? Path.GetFullPath(name) : null;

        var exts = new List<string> { "" };
        if (OperatingSystem.IsWindows())
        {
            var pe = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
                     .Split(';', StringSplitOptions.RemoveEmptyEntries);
            exts.InsertRange(0, pe); // prefer .EXE/.CMD over an extensionless match on Windows
        }
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "")
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            foreach (var ext in exts)
            {
                string cand = Path.Combine(dir, name + ext);
                if (File.Exists(cand)) return cand;
            }
        return null;
    }

    private static string SafeVersion(string cli)
    {
        try { return RunCaptured(cli, new[] { "--version" }, null, null, out var v) == 0
                     ? v.Trim().Split('\n')[0].Trim() : ""; }
        catch { return ""; }
    }

    // ── usage / provenance ──────────────────────────────────────────────────

    /// <summary>Reads token/cost usage from Claude Code's `--output-format json` envelope (defensive).</summary>
    private static (long? tokensIn, long? tokensOut, double? cost) ReadClaudeUsage(string stdout)
    {
        try
        {
            int s = stdout.IndexOf('{'), e = stdout.LastIndexOf('}');
            if (s < 0 || e <= s) return (null, null, null);
            using var doc = JsonDocument.Parse(stdout.Substring(s, e - s + 1));
            var root = doc.RootElement;
            double? cost = GetDouble(root, "total_cost_usd") ?? GetDouble(root, "cost_usd");
            long? ti = null, to = null;
            if (root.TryGetProperty("usage", out var u) && u.ValueKind == JsonValueKind.Object)
            {
                ti = GetLong(u, "input_tokens") ?? GetLong(u, "prompt_tokens");
                to = GetLong(u, "output_tokens") ?? GetLong(u, "completion_tokens");
            }
            return (ti, to, cost);
        }
        catch { return (null, null, null); }
    }

    private static void WriteMeta(string path, string harness, string harnessVersion, string effort,
        int durationSec, int passes, long? tokensIn, long? tokensOut, double? costUsd, string promptVersion)
    {
        using var fs = File.Create(path);
        using var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        w.WriteStartObject();
        w.WriteString("harness", harness);
        w.WriteString("harnessVersion", harnessVersion);
        w.WriteString("effort", effort);
        w.WriteNumber("durationSec", durationSec);
        w.WriteNumber("passes", passes);
        w.WriteNumber("attempts", 1);
        if (tokensIn is long ti) w.WriteNumber("tokensIn", ti); else w.WriteNull("tokensIn");
        if (tokensOut is long to) w.WriteNumber("tokensOut", to); else w.WriteNull("tokensOut");
        if (costUsd is double c) w.WriteNumber("costUsd", c); else w.WriteNull("costUsd");
        w.WriteString("promptVersion", promptVersion);
        w.WriteString("producedAtUtc", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        w.WriteString("notes", "");
        w.WriteEndObject();
    }

    private static string PromptVersion(string repo, bool noReview)
    {
        string files = noReview ? "PROMPT.md" : "PROMPT.md + PROMPT-REVIEW.md";
        try
        {
            var pathArgs = noReview
                ? new[] { "-C", repo, "log", "-1", "--format=%h", "--", "PROMPT.md" }
                : new[] { "-C", repo, "log", "-1", "--format=%h", "--", "PROMPT.md", "PROMPT-REVIEW.md" };
            if (RunCaptured("git", pathArgs, null, null, out var sha) == 0 && sha.Trim().Length > 0)
                return $"{files}@{sha.Trim()}";
        }
        catch { }
        return files;
    }

    // ── misc ─────────────────────────────────────────────────────────────────

    private static int NextRunNumber(string modelDir)
    {
        int max = 0;
        if (Directory.Exists(modelDir))
            foreach (var d in Directory.EnumerateDirectories(modelDir))
            {
                var n = Path.GetFileName(d);
                if (n.StartsWith("run", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(n.AsSpan(3), out int v) && v > max) max = v;
            }
        return max + 1;
    }

    private static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PROMPT.md")) &&
                Directory.Exists(Path.Combine(dir.FullName, "submissions")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return start; // fall back to CWD; PROMPT.md check downstream will complain if wrong
    }

    /// <summary>agy blocks on a "trust this folder?" prompt; pre-trust the workdir like generate.ps1 did.</summary>
    private static void EnsureAgyTrusted(string dir)
    {
        try
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string tf = Path.Combine(home, ".gemini", "trustedFolders.json");
            if (!File.Exists(tf)) return; // agy not configured — nothing to do
            using var doc = JsonDocument.Parse(File.ReadAllText(tf));
            var map = new Dictionary<string, string>();
            foreach (var p in doc.RootElement.EnumerateObject()) map[p.Name] = p.Value.GetString() ?? "TRUST_FOLDER";
            if (map.ContainsKey(dir)) return;
            map[dir] = "TRUST_FOLDER";
            File.WriteAllText(tf, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best-effort; if it fails agy will just prompt */ }
    }

    private static double? GetDouble(JsonElement o, string name)
        => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
    private static long? GetLong(JsonElement o, string name)
        => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : null;

    private static string Next(string[] a, ref int i, string opt)
        => ++i < a.Length ? a[i] : throw new UsageException($"{opt} needs a value");

    private static void PrintList()
    {
        Console.WriteLine("model      cli     model-id                       effort  harness");
        foreach (var (k, s) in Matrix)
            Console.WriteLine($"{k,-10} {s.Cli,-7} {s.Model,-30} {(s.Effort.Length>0?s.Effort:"-"),-7} {s.Harness}");
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"model-runner — generate one benchmark run (2-pass) and stamp its provenance.

usage:
  dotnet run --project model-runner -- <model> [options]

options:
  --effort <e>   override the model's default reasoning effort
  --run <n>      force the run number (default: next available)
  --no-review    single-pass only (skip PROMPT-REVIEW.md)
  --repo <path>  repo root (default: found by walking up for PROMPT.md + submissions/)
  --dry-run      print the plan, run nothing
  --list         list the model matrix
  -h, --help     this help

flow (per run):
  pass 1  feed PROMPT.md            -> model builds submissions/<model>/<runN>/
  pass 2  feed PROMPT.md+REVIEW     -> model reviews, runs, validates, patches (same folder)
  then    write submissions/<model>/<runN>.meta.json (harness, effort, duration, passes, tokens, cost)

example:
  dotnet run --project model-runner -- sonnet-5
  dotnet run --project model-runner -- gpt-5-5 --effort xhigh

note:
  the model CLI must be signed in. for claude, an ANTHROPIC_API_KEY in the environment
  takes precedence over your claude.ai login — if it has no credit the run fails. unset it
  to use your claude.ai subscription (esp. when running inside another Claude Code session).");
    }

    // ── types ────────────────────────────────────────────────────────────────

    private sealed record ModelSpec(string Cli, string Model, string Effort, string Harness);

    private readonly record struct Usage(int Exit, long? TokensIn, long? TokensOut, double? CostUsd);

    private sealed class UsageException(string message) : Exception(message);
}
