using BackendEvaluator.Core;
using BackendEvaluator.Evaluators;
using BackendEvaluator.Reporting;

namespace BackendEvaluator.Cli;

/// <summary>Command-line entry logic: parses args, runs the 13 evaluators and writes the reports.</summary>
public static class Runner
{
    public static async Task<int> Run(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        bool deep = args.Contains("--deep");
        string? output = GetOption(args, "--output");
        string? baseUrl = GetOption(args, "--base-url") ?? Environment.GetEnvironmentVariable("BENCH_BASE_URL");
        string? ingest = GetOption(args, "--ingest") ?? Environment.GetEnvironmentVariable("BENCH_INGEST_DIR");
        var positionals = args.Where(a => !a.StartsWith('-')).ToList();
        foreach (var v in new[] { output, baseUrl, ingest }) // strip values that followed an option flag
            if (v != null) positionals.Remove(v);

        string repoRoot = FindUp(AppContext.BaseDirectory, d => Directory.Exists(Path.Combine(d, "submissions")))
                          ?? FindUp(Directory.GetCurrentDirectory(), d => Directory.Exists(Path.Combine(d, "submissions")))
                          ?? Directory.GetCurrentDirectory();

        // Aggregate existing reports into a per-model ranking (no target needed).
        if (args.Contains("--leaderboard"))
            return RunLeaderboard(output ?? Path.Combine(repoRoot, "evaluator-dotnet", "results"));

        if (positionals.Count == 0)
        {
            Console.WriteLine("Error: provide the target project path (or a submission name).");
            Console.WriteLine();
            ListSubmissions(repoRoot);
            PrintUsage();
            return 2;
        }

        string targetArg = positionals[0];
        string? targetPath = ResolveTarget(targetArg, repoRoot);
        if (targetPath == null)
        {
            Console.WriteLine($"Error: target not found: '{targetArg}'");
            ListSubmissions(repoRoot);
            return 2;
        }

        string displayName = MakeDisplayName(targetPath, repoRoot);
        string outDir = output ?? Path.Combine(repoRoot, "evaluator-dotnet", "results");
        Directory.CreateDirectory(outDir);

        var tools = new ToolRunner();
        var options = new EvaluatorOptions
        {
            TargetPath = targetPath,
            Deep = deep,
            OutputDir = outDir,
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            IngestDir = string.IsNullOrWhiteSpace(ingest) ? null : ingest,
        };
        ContractReport? contract = null;
        bool? booted = null;
        if (options.BaseUrl != null)
        {
            Console.WriteLine($"  waiting for the system under test at {options.BaseUrl} ...");
            booted = HttpProbe.WaitForOk(options.BaseUrl.TrimEnd('/') + "/health", 180_000, Console.WriteLine);
            if (booted == true)
            {
                Console.WriteLine("  driving the live API contract oracle ...");
                try { contract = ContractOracle.Run(options.BaseUrl, Console.WriteLine); }
                catch (Exception ex) { Console.WriteLine($"  contract oracle error: {ex.Message}"); }
                if (contract is { Reachable: false })
                    Console.WriteLine("  contract oracle: could not resolve the API routes (credit-cards/transactions).");
            }
            else Console.WriteLine("  service did not become healthy at /health — skipping the live oracle.");
        }

        Console.WriteLine($"Inspecting {targetPath} ...");
        var project = new ProjectInspector(targetPath);
        Console.WriteLine($"  {project.SourceFiles.Count} .cs files, {project.CsprojFiles.Count} .csproj");

        Console.WriteLine("  parsing C# with Roslyn (AST) ...");
        var facts = RoslynAnalyzer.Analyze(project);
        Console.WriteLine(facts.Available
            ? $"  Roslyn engine: parsed {facts.FilesParsed} file(s){(facts.ParseErrors > 0 ? $", {facts.ParseErrors} parse error(s)" : "")}"
            : "  Roslyn engine: no parseable C# found (code categories will be n/a)");

        // Resolve the solution/project once: the deep dotnet commands (build gate, test, format) target it,
        // since running them with no argument is ambiguous in a multi-project target.
        var sln = project.FindByNamePattern(@"\.slnx?$").FirstOrDefault();

        var ctx = new EvaluationContext
        {
            Project = project,
            Facts = facts,
            Tools = tools,
            Options = options,
            Contract = contract,
            Log = Console.WriteLine,
            SolutionPath = sln,
        };

        var report = new EvaluationReport
        {
            Target = displayName,
            EvaluatedAtUtc = DateTime.UtcNow.ToString("u"),
            Deep = deep,
        };
        report.Environment.AddRange(DetectTools(tools));
        report.Boots = booted;

        // Executability gate (deep): does the source actually compile? A non-building submission cannot
        // be production-grade, so its score is capped later regardless of how clean the source reads.
        if (deep && tools.IsAvailable("dotnet"))
        {
            Console.WriteLine("  checking the project builds (dotnet build) ...");
            // -clp:Summary surfaces the "N Warning(s)" line so this single Release build also feeds
            // CodeQuality's build-warnings metric (no second build is run anywhere).
            string buildArgs = sln != null
                ? $"build \"{sln}\" -c Release --nologo -clp:Summary"
                : "build -c Release --nologo -clp:Summary";
            var build = tools.Run("dotnet", buildArgs, targetPath, 600_000);
            report.Builds = build.Success;
            var warnMatch = System.Text.RegularExpressions.Regex.Match(build.Combined, @"(\d+)\s+Warning\(s\)");
            if (warnMatch.Success) ctx.BuildWarnings = int.Parse(warnMatch.Groups[1].Value);
            Console.WriteLine($"  build: {(report.Builds == true ? "ok" : "FAILED")}");
        }

        foreach (var ev in EvaluatorRegistry.All)
        {
            Console.WriteLine($"  -> [{ev.Number,2}] {ev.Name} ...");
            try
            {
                var result = await ev.EvaluateAsync(ctx);
                report.Categories.Add(result);
            }
            catch (Exception ex)
            {
                var failed = new CategoryResult
                {
                    Number = ev.Number, Name = ev.Name, Weight = ev.Weight, Automation = ev.Automation,
                };
                failed.Notes.Add($"ERROR while evaluating: {ex.Message}");
                report.Categories.Add(failed);
            }
        }

        // Fold in results dropped by sidecar tool containers (e.g. OWASP ZAP).
        if (options.IngestDir != null && Directory.Exists(options.IngestDir))
            IngestSidecars(report, options.IngestDir);

        // Weighted final over categories that produced a score (renormalized), on the 0..5 scale.
        (report.WeightedScore, report.Coverage) = Scoring.Aggregate(report.Categories);

        // Cap the headline when the submission failed the executability gate. A deep score is only
        // trustworthy if the system actually ran and was verified live — so no runnable compose, or no
        // observed healthy boot, caps the score hard (see Scoring.CapForExecutability). The run is the
        // measurement; static signals alone cannot certify a project works.
        bool hasRunnableSystem = project.AnyFile("docker-compose.yml", "docker-compose.yaml", "compose.yml", "compose.yaml");
        (report.WeightedScore, report.ScoreCapReason) = Scoring.CapForExecutability(
            report.WeightedScore, report.Builds, report.Boots, deep, hasRunnableSystem);
        if (report.ScoreCapReason != null) Console.WriteLine($"  {report.ScoreCapReason}");

        // A run minimally patched to build/boot is graded on its merits, then docked (bench-patch.json).
        if (ReadPatchMarker(targetPath) is { } patch)
        {
            report.PatchPenalty = patch.points;
            report.PatchReason = patch.reason;
            report.WeightedScore = Scoring.ApplyPatchPenalty(report.WeightedScore, patch.points);
            Console.WriteLine($"  patch penalty: -{patch.points:0.0} ({patch.reason})");
        }

        ConsoleReporter.Print(report, Console.WriteLine);

        string baseName = Sanitize(displayName);
        string jsonPath = JsonReporter.Write(report, Path.Combine(outDir, baseName + ".dotnet.json"));
        string mdPath = MarkdownReporter.Write(report, Path.Combine(outDir, baseName + ".dotnet.md"));
        Console.WriteLine($"Reports: {jsonPath}");
        Console.WriteLine($"         {mdPath}");
        return 0;
    }

    /// <summary>Reads result files written by sidecar tool containers and attaches them as metrics.</summary>
    private static void IngestSidecars(EvaluationReport report, string dir)
    {
        IngestKafka(report, dir);

        // OWASP ZAP baseline: exit 0 = clean, 2 = warnings, 1/3 = fail. Written to zap.exit by the ZAP service.
        var zapExit = Path.Combine(dir, "zap.exit");
        if (File.Exists(zapExit))
        {
            var cat = report.Categories.FirstOrDefault(c => c.Number == 7);
            if (cat != null)
            {
                int code = int.TryParse(File.ReadAllText(zapExit).Trim(), out var x) ? x : -1;
                var metric = code switch
                {
                    0 => new MetricResult { Name = "dast-zap", Observed = "no alerts", Target = "OWASP ZAP baseline clean", Status = MetricStatus.Pass },
                    2 => new MetricResult { Name = "dast-zap", Observed = "warnings", Target = "OWASP ZAP baseline clean", Status = MetricStatus.Partial, Note = "review ZAP warnings" },
                    -1 => new MetricResult { Name = "dast-zap", Observed = "unreadable", Target = "OWASP ZAP baseline clean", Status = MetricStatus.Indeterminate },
                    _ => new MetricResult { Name = "dast-zap", Observed = "alerts found", Target = "OWASP ZAP baseline clean", Status = MetricStatus.Fail, Note = "review ZAP findings" },
                };
                cat.Metrics.Add(metric);
            }
        }
    }

    /// <summary>
    /// Folds the live Kafka observation (from the harness <c>kafka-check</c> consumer) into category 6:
    /// confirms a transaction event actually landed on the <c>transactions</c> topic, keyed by its id.
    /// File format: a <c>#connected</c> marker line once the broker is reachable, then <c>key\tvalue</c>
    /// per consumed message.
    /// </summary>
    private static void IngestKafka(EvaluationReport report, string dir)
    {
        var file = Path.Combine(dir, "kafka.events");
        if (!File.Exists(file)) return;
        var cat = report.Categories.FirstOrDefault(c => c.Number == 6);
        if (cat == null) return;

        var lines = File.ReadAllLines(file);
        bool connected = lines.Any(l => l.Trim() == "#connected");
        var events = lines.Where(l => l.Length > 0 && !l.StartsWith('#') && l.Contains('\t')).ToList();

        MetricResult metric;
        if (!connected)
            metric = new MetricResult { Name = "kafka-event-live", Observed = "broker unreachable", Target = "transaction event published to 'transactions'", Status = MetricStatus.Indeterminate, Note = "harness kafka-check could not reach the broker" };
        else if (events.Count == 0)
            metric = new MetricResult { Name = "kafka-event-live", Observed = "no events", Target = "transaction event published to 'transactions'", Status = MetricStatus.Fail, Note = "no message observed on the topic during the run" };
        else if (events.Any(l => KeyMatchesId(l)))
            metric = new MetricResult { Name = "kafka-event-live", Observed = $"{events.Count} event(s), key=id", Target = "transaction event published, keyed by id", Status = MetricStatus.Pass };
        else
            metric = new MetricResult { Name = "kafka-event-live", Observed = $"{events.Count} event(s), key!=id", Target = "transaction event published, keyed by id", Status = MetricStatus.Partial, Note = "event seen but the key is not the transaction id" };

        cat.Metrics.Add(metric);
    }

    /// <summary>A <c>key\tvalue</c> line where the message key equals the JSON value's <c>id</c>.</summary>
    private static bool KeyMatchesId(string line)
    {
        var parts = line.Split('\t', 2);
        if (parts.Length != 2) return false;
        var key = parts[0].Trim();
        if (key.Length == 0) return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(parts[1]);
            foreach (var prop in doc.RootElement.EnumerateObject())
                if (string.Equals(prop.Name, "id", StringComparison.OrdinalIgnoreCase))
                {
                    var idStr = prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number
                        ? prop.Value.GetRawText()
                        : prop.Value.GetString();
                    return string.Equals(idStr, key, StringComparison.Ordinal);
                }
        }
        catch { /* not JSON — treat as no match */ }
        return false;
    }

    private static readonly System.Text.Json.JsonSerializerOptions LeaderboardJson = new() { PropertyNameCaseInsensitive = true };
    private sealed record ReportDto(string? Target, double? WeightedScore, bool Deep);

    /// <summary>Aggregates the *.dotnet.json reports in a directory into a per-model median ranking.</summary>
    private static int RunLeaderboard(string dir)
    {
        if (!Directory.Exists(dir)) { Console.WriteLine($"No results directory: {dir}"); return 2; }

        var runs = new List<RunScore>();
        int excludedLight = 0;
        foreach (var file in Directory.GetFiles(dir, "*.dotnet.json"))
        {
            ReportDto? dto;
            try { dto = System.Text.Json.JsonSerializer.Deserialize<ReportDto>(File.ReadAllText(file), LeaderboardJson); }
            catch { continue; }
            if (dto?.Target == null || dto.WeightedScore == null) continue;
            if (!dto.Deep) { excludedLight++; continue; }   // light runs aren't comparable
            var (model, run) = SplitTarget(dto.Target);
            runs.Add(new RunScore(model, run, dto.WeightedScore.Value));
        }

        if (runs.Count == 0)
        {
            Console.WriteLine($"No comparable (deep, scored) reports in {dir}.");
            if (excludedLight > 0) Console.WriteLine($"  ({excludedLight} light run(s) skipped — re-run with --deep to rank them.)");
            return 0;
        }

        var rows = Leaderboard.Aggregate(runs);
        string path = LeaderboardReporter.Write(rows, Path.Combine(dir, "leaderboard.dotnet.md"), excludedLight, DateTime.UtcNow.ToString("u"));

        Console.WriteLine();
        Console.WriteLine("  LEADERBOARD (deep runs, ranked by median /5)");
        int rank = 1;
        foreach (var r in rows.Where(x => !x.IsBaseline))
            Console.WriteLine($"   {rank++,2}. {r.Model,-30} {r.Median:0.00}/5  (n={r.Count}{(r.Provisional ? ", provisional" : "")})");
        if (excludedLight > 0) Console.WriteLine($"  ({excludedLight} light run(s) excluded.)");
        Console.WriteLine($"  Written: {path}");
        return 0;
    }

    private static (string model, string run) SplitTarget(string target)
    {
        var norm = target.Replace('\\', '/').TrimEnd('/');
        int i = norm.LastIndexOf('/');
        return i < 0 ? (norm, "run1") : (norm[..i], norm[(i + 1)..]);
    }

    /// <summary>Reads a submission's bench-patch.json marker: {"points": &lt;0..5&gt;, "reason": "..."}.</summary>
    private static (double points, string reason)? ReadPatchMarker(string targetPath)
    {
        var file = Path.Combine(targetPath, "bench-patch.json");
        if (!File.Exists(file)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            double points = root.TryGetProperty("points", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.Number ? p.GetDouble() : 0;
            string reason = root.TryGetProperty("reason", out var r) && r.ValueKind == System.Text.Json.JsonValueKind.String ? r.GetString() ?? "" : "minimal build/boot patch";
            return points > 0 ? (points, reason) : null;
        }
        catch { return null; }
    }

    private static IEnumerable<string> DetectTools(ToolRunner tools)
    {
        foreach (var tool in ToolCatalog.Tools.Keys)
            if (tools.IsAvailable(tool, ToolCatalog.Probe(tool))) yield return tool;
    }

    private static string? ResolveTarget(string arg, string repoRoot)
    {
        if (Directory.Exists(arg)) return Path.GetFullPath(arg);
        var underSubs = Path.Combine(repoRoot, "submissions", arg.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(underSubs)) return Path.GetFullPath(underSubs);
        var underRoot = Path.Combine(repoRoot, arg.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(underRoot)) return Path.GetFullPath(underRoot);
        return null;
    }

    private static string MakeDisplayName(string targetPath, string repoRoot)
    {
        var subs = Path.Combine(repoRoot, "submissions");
        if (targetPath.StartsWith(subs, StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(subs, targetPath).Replace('\\', '/');
        return Path.GetFileName(targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static void ListSubmissions(string repoRoot)
    {
        var subs = Path.Combine(repoRoot, "submissions");
        if (!Directory.Exists(subs)) return;
        var names = new List<string>();
        foreach (var top in Directory.GetDirectories(subs))
        {
            var runs = Directory.GetDirectories(top);
            if (runs.Length == 0) names.Add(Path.GetFileName(top));
            foreach (var run in runs) names.Add($"{Path.GetFileName(top)}/{Path.GetFileName(run)}");
        }
        if (names.Count > 0)
        {
            Console.WriteLine("Available submissions:");
            foreach (var n in names.OrderBy(x => x)) Console.WriteLine($"  - {n}");
            Console.WriteLine();
        }
    }

    private static string? GetOption(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length) return args[i + 1];
            if (args[i].StartsWith(name + "=")) return args[i][(name.Length + 1)..];
        }
        return null;
    }

    private static string? FindUp(string start, Func<string, bool> pred)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            try { if (pred(dir.FullName)) return dir.FullName; } catch { }
            dir = dir.Parent;
        }
        return null;
    }

    private static string Sanitize(string s)
    {
        var clean = s.Replace('/', '_').Replace('\\', '_');
        foreach (var ch in Path.GetInvalidFileNameChars()) clean = clean.Replace(ch, '_');
        return clean;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
        Usage:
          Evaluator <path-or-submission> [--deep] [--base-url <url>] [--output <dir>]
          Evaluator --leaderboard [--output <dir>]

        Arguments:
          <path-or-submission>  Target project folder, or a submission name
                                (e.g. "claude-haiku-4-5/run1").
          --deep                Also run the heavy/dynamic tools
                                (dotnet build gate, dotnet test, coverage, format, SCA, external lints).
                                Default mode (light) does static analysis + detection only.
          --base-url <url>      Live system under test; runs the contract oracle + live probes.
          --leaderboard         Aggregate existing *.dotnet.json reports into a per-model median
                                ranking (deep runs only) and write leaderboard.dotnet.md.
          --output <dir>        Reports folder (default: evaluator-dotnet/results).
          -h, --help            Show this help.

        Output: console report + <target>.dotnet.json + <target>.dotnet.md
        Automation legend: full-auto (green) | semi/oracle (yellow) | proxy+review (orange).
        """);
    }
}
