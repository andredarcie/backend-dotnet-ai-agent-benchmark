using BackendEvaluator.Core;

namespace BackendEvaluator.Reporting;

public static class ConsoleReporter
{
    private static string Sym(MetricStatus s) => s switch
    {
        MetricStatus.Pass => "[+]",
        MetricStatus.Partial => "[~]",
        MetricStatus.Fail => "[-]",
        _ => "[?]",
    };

    private static string Yn(bool? b) => b switch { true => "yes", false => "NO", _ => "n/a" };

    public static void Print(EvaluationReport report, Action<string> w)
    {
        w("");
        w("================================================================");
        w($"  Backend Evaluator (.NET 10) — {report.Target}");
        w($"  {report.EvaluatedAtUtc}  |  mode: {(report.Deep ? "deep" : "light")}");
        w("================================================================");
        if (report.Environment.Count > 0)
            w("  Local tools: " + string.Join(", ", report.Environment));
        if (report.Builds.HasValue || report.Boots.HasValue)
            w($"  Executable: build={Yn(report.Builds)}  boot={Yn(report.Boots)}");
        w("");

        foreach (var c in report.Categories)
        {
            string score = c.Score.HasValue ? $"{c.Score:0.0}/5" : "n/a";
            w($"#{c.Number,2} [{c.Automation.Label(),-14}] {c.Name}");
            w($"      score: {score}   weight: {c.Weight * 100:0.#}%");
            foreach (var m in c.Metrics)
            {
                w($"      {Sym(m.Status)} {m.Name}: {m.Observed}  (target: {m.Target})");
                if (!string.IsNullOrEmpty(m.Note)) w($"            . {m.Note}");
            }
            foreach (var n in c.Notes) w($"      i {n}");
            if (c.MissingTools.Count > 0) w($"      ! missing tools: {string.Join(", ", c.MissingTools)}");
            w("");
        }

        w("----------------------------------------------------------------");
        string final = report.WeightedScore.HasValue ? $"{report.WeightedScore:0.00}/5" : "n/a";
        w($"  WEIGHTED FINAL SCORE: {final}   (coverage: {report.Coverage:P0} of categories)");
        if (report.ScoreCapReason != null)
            w($"  /!\\ SCORE CAPPED: {report.ScoreCapReason}");
        if (!report.Deep)
            w("  NOTE: light mode (static only) — not comparable to deep/harness scores.");
        var notScored = report.Categories.Where(c => !c.Score.HasValue).Select(c => $"#{c.Number}").ToList();
        if (notScored.Count > 0)
            w($"  Not scored (missing tool/Docker or --deep): {string.Join(", ", notScored)}");
        w("----------------------------------------------------------------");
        w("");
    }
}
