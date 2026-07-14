using System.Globalization;
using System.Xml.Linq;

namespace BackendEvaluator.Core;

/// <summary>
/// Merges multiple Coverlet <c>coverage.cobertura.xml</c> reports into a single line-coverage figure for
/// <b>the code that matters</b> — which is exactly what the rubric asks for, and what a raw line-rate does
/// not give you.
///
/// <para><b>Union, not "the newest file".</b> Each test project emits its OWN report and they cover
/// different assemblies, so reading any single report in isolation understates the real coverage — and a
/// suite that could not run leaves an empty report (line-rate 0), which used to silently report 0% for a
/// well-tested project. A source line counts as covered if ANY report recorded a hit on it.</para>
///
/// <para><b>Generated code and the composition root are excluded from the denominator</b> (see
/// <see cref="IsExcluded"/>). They are lines no unit test can cover <i>by construction</i>, and counting
/// them punishes a submission for following the brief. In a real measurement this dominated the number:
/// a submission with <b>72% coverage of its own code</b> was scored <b>32%</b> — a Fail — because the
/// denominator carried ~1,350 uncoverable lines of EF migration scaffolding (which the task
/// <i>requires</i>), source-generated OpenAPI code under <c>obj/</c>, and <c>Program.cs</c> (which the
/// task's own unit-only rule — no <c>WebApplicationFactory</c> — makes untestable). That is the evaluator
/// grading the framework, not the model.</para>
///
/// <para><b>Not gameable.</b> The exclusions are applied by the EVALUATOR on objective, uniform criteria —
/// they are not read from the submission's own <c>coverlet.runsettings</c>, which a model could otherwise
/// widen until everything untested was excluded. And exclusion is reported alongside the number
/// (<see cref="Result.Excluded"/>) so any run can be audited.</para>
/// </summary>
public static class CoberturaCoverage
{
    public sealed record Result(int Covered, int Coverable, int Reports, int Excluded)
    {
        public bool Any => Reports > 0 && Coverable > 0;
        public double LineRate => Coverable > 0 ? (double)Covered / Coverable : 0;
    }

    /// <summary>Union line coverage across every given cobertura report, over coverable code only.</summary>
    public static Result Merge(IEnumerable<string> coberturaFiles)
    {
        // key: "<filename>:<lineNumber>" -> covered by at least one report.
        var lines = new Dictionary<string, bool>();
        var excluded = new HashSet<string>();
        int reports = 0;
        foreach (var file in coberturaFiles)
        {
            XDocument doc;
            try { doc = XDocument.Load(file); }
            catch { continue; }
            reports++;
            foreach (var cls in doc.Descendants("class"))
            {
                var filename = (string?)cls.Attribute("filename") ?? "";
                bool skip = IsExcluded(filename);
                foreach (var line in cls.Descendants("line"))
                {
                    var number = (string?)line.Attribute("number");
                    if (number == null) continue;
                    var key = filename + ":" + number;
                    if (skip) { excluded.Add(key); continue; }
                    bool covered = Hits(line) > 0;
                    lines[key] = lines.TryGetValue(key, out var prev) ? (prev || covered) : covered;
                }
            }
        }
        return new Result(lines.Values.Count(v => v), lines.Count, reports, excluded.Count);
    }

    /// <summary>
    /// Lines a unit test cannot cover by construction, so they must not sit in the denominator:
    /// <list type="bullet">
    /// <item><b>obj/</b> — build output. Source generators (e.g. the OpenAPI XML-comment generator) emit
    /// hundreds of lines here. The model did not write them and cannot test them.</item>
    /// <item><b>Migrations/</b> — EF scaffolding emitted by <c>dotnet ef</c>, including the model snapshot.
    /// The task <i>mandates</i> migrations, so this penalty applied to every compliant submission.</item>
    /// <item><b>Program.cs</b> — the composition root. Exercising it needs the host to boot, i.e.
    /// <c>WebApplicationFactory</c> — which the task explicitly forbids. It is covered instead by the
    /// evaluator's own live contract oracle, which drives the real running app.</item>
    /// </list>
    /// Anything else — services, controllers, entities, repositories, middleware, the Kafka publisher —
    /// stays in the denominator. Not testing those still costs the submission, exactly as it should.
    /// </summary>
    private static bool IsExcluded(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return true;   // no source file => generated
        var f = filename.Replace('\\', '/');
        return f.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               || f.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
               || f.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
               || f.StartsWith("Migrations/", StringComparison.OrdinalIgnoreCase)
               || f.EndsWith("/Program.cs", StringComparison.OrdinalIgnoreCase)
               || f.Equals("Program.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static int Hits(XElement line)
    {
        var h = (string?)line.Attribute("hits");
        return int.TryParse(h, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }
}
