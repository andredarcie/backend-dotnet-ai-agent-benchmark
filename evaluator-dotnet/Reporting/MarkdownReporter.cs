using System.Text;
using BackendEvaluator.Core;

namespace BackendEvaluator.Reporting;

public static class MarkdownReporter
{
    private static string Yn(bool? b) => b switch { true => "yes", false => "**NO**", _ => "n/a" };

    public static string Write(EvaluationReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Evaluation Report — {report.Target}");
        sb.AppendLine();
        sb.AppendLine($"- **Date (UTC):** {report.EvaluatedAtUtc}");
        sb.AppendLine($"- **Mode:** {(report.Deep ? "deep" : "light")}");
        string final = report.WeightedScore.HasValue ? $"{report.WeightedScore:0.00}/5" : "n/a";
        sb.AppendLine($"- **Weighted final score:** {final}");
        if (report.ScoreCapReason != null)
            sb.AppendLine($"- **⚠️ Score capped:** {report.ScoreCapReason}");
        sb.AppendLine($"- **Coverage:** {report.Coverage:P0} of categories");
        if (report.Builds.HasValue || report.Boots.HasValue)
            sb.AppendLine($"- **Executable:** build = {Yn(report.Builds)} · boot (/health) = {Yn(report.Boots)}");
        if (report.Environment.Count > 0)
            sb.AppendLine($"- **Local tools detected:** {string.Join(", ", report.Environment)}");
        if (!report.Deep)
            sb.AppendLine("- _Light mode: static analysis only — **not comparable** to deep/harness runs._");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| # | Category | Measure | Score | Weight |");
        sb.AppendLine("|---|----------|---------|-------|--------|");
        foreach (var c in report.Categories)
        {
            string score = c.Score.HasValue ? $"{c.Score:0.0}/5" : "n/a";
            sb.AppendLine($"| {c.Number} | {c.Name} | {c.Automation.Badge()} | {score} | {c.Weight * 100:0.#}% |");
        }
        sb.AppendLine();

        foreach (var c in report.Categories)
        {
            sb.AppendLine($"## {c.Number}. {c.Name} {c.Automation.Badge()}");
            sb.AppendLine();
            string score = c.Score.HasValue ? $"{c.Score:0.0}/5" : "n/a (not measured)";
            sb.AppendLine($"**Score:** {score} · **Weight:** {c.Weight * 100:0.#}% · **Automation:** {c.Automation.Label()}");
            sb.AppendLine();
            sb.AppendLine("| Status | Metric | Observed | Target |");
            sb.AppendLine("|--------|--------|----------|--------|");
            foreach (var m in c.Metrics)
            {
                string s = m.Status switch
                {
                    MetricStatus.Pass => "✅",
                    MetricStatus.Partial => "🟨",
                    MetricStatus.Fail => "❌",
                    _ => "❔",
                };
                string note = string.IsNullOrEmpty(m.Note) ? "" : $"<br/>_{m.Note}_";
                sb.AppendLine($"| {s} | {m.Name} | {m.Observed}{note} | {m.Target} |");
            }
            sb.AppendLine();
            foreach (var n in c.Notes) sb.AppendLine($"> ℹ️ {n}");
            if (c.MissingTools.Count > 0) sb.AppendLine($"> ⚠️ Missing tools: {string.Join(", ", c.MissingTools)}");
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
