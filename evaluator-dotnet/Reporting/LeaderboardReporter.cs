using System.Text;
using BackendEvaluator.Core;

namespace BackendEvaluator.Reporting;

/// <summary>Renders the per-model ranking (median ± spread) to Markdown.</summary>
public static class LeaderboardReporter
{
    public static string Write(IReadOnlyList<ModelStats> rows, string path, int excludedLightRuns, string evaluatedAtUtc)
    {
        var ranked = rows.Where(r => !r.IsBaseline).ToList();
        var baselines = rows.Where(r => r.IsBaseline).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# Leaderboard (evaluator-dotnet)");
        sb.AppendLine();
        sb.AppendLine($"- **Generated (UTC):** {evaluatedAtUtc}");
        sb.AppendLine("- Ranked by **per-model median** of the weighted score (0–5). Only **deep** runs are");
        sb.AppendLine("  counted (light/static-only runs aren't comparable).");
        bool anyProvisional = ranked.Any(r => r.Provisional);
        if (anyProvisional)
            sb.AppendLine($"- ⚠️ Models with **< {Leaderboard.ProvisionalThreshold} runs** are **provisional** — treat gaps within the spread as ties.");
        if (excludedLightRuns > 0)
            sb.AppendLine($"- _{excludedLightRuns} light run(s) excluded from the ranking._");
        sb.AppendLine();

        sb.AppendLine("| # | Model | Runs | Median /5 | Spread (mean ±σ, range) |");
        sb.AppendLine("|--:|-------|:---:|:---------:|--------------------------|");
        int rank = 1;
        foreach (var r in ranked)
        {
            string flag = r.Provisional ? " ⚠" : "";
            string spread = r.Count > 1
                ? $"{r.Mean:0.00} ±{r.StdDev:0.00} ({r.Min:0.0}–{r.Max:0.0})"
                : "single run";
            sb.AppendLine($"| {rank++} | `{r.Model}`{flag} | {r.Count} | **{r.Median:0.00}** | {spread} |");
        }
        sb.AppendLine();

        if (baselines.Count > 0)
        {
            sb.AppendLine("## Reference baselines (not ranked)");
            sb.AppendLine();
            sb.AppendLine("| Baseline | Runs | Median /5 |");
            sb.AppendLine("|----------|:---:|:---------:|");
            foreach (var b in baselines)
                sb.AppendLine($"| `{b.Model}` | {b.Count} | {b.Median:0.00} |");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("> ℹ️ No reference baseline scored yet. Add a known-good solution under");
            sb.AppendLine("> `submissions/_baseline/run1` to anchor the scale (and, ideally, a deliberately-weak control).");
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
