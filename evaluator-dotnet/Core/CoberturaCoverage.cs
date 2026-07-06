using System.Globalization;
using System.Xml.Linq;

namespace BackendEvaluator.Core;

/// <summary>
/// Merges multiple Coverlet <c>coverage.cobertura.xml</c> reports into a single line-coverage figure.
/// Each test project emits its OWN report (a unit suite and an integration suite cover different
/// assemblies), so reading any single report in isolation understates the real coverage — and when one
/// suite could not run (e.g. Testcontainers had no Docker), its report is empty (line-rate 0). Picking
/// "the newest file" therefore silently reported 0% for a well-tested project. The correct combined
/// figure is the UNION: a source line counts as covered if ANY report recorded a hit on it.
/// </summary>
public static class CoberturaCoverage
{
    public sealed record Result(int Covered, int Coverable, int Reports)
    {
        public bool Any => Reports > 0 && Coverable > 0;
        public double LineRate => Coverable > 0 ? (double)Covered / Coverable : 0;
    }

    /// <summary>Union line coverage across every given cobertura report.</summary>
    public static Result Merge(IEnumerable<string> coberturaFiles)
    {
        // key: "<filename>:<lineNumber>" -> covered by at least one report.
        var lines = new Dictionary<string, bool>();
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
                foreach (var line in cls.Descendants("line"))
                {
                    var number = (string?)line.Attribute("number");
                    if (number == null) continue;
                    var key = filename + ":" + number;
                    bool covered = Hits(line) > 0;
                    lines[key] = lines.TryGetValue(key, out var prev) ? (prev || covered) : covered;
                }
            }
        }
        return new Result(lines.Values.Count(v => v), lines.Count, reports);
    }

    private static int Hits(XElement line)
    {
        var h = (string?)line.Attribute("hits");
        return int.TryParse(h, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }
}
