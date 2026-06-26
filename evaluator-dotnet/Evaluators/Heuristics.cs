using System.Text.RegularExpressions;

namespace BackendEvaluator.Evaluators;

/// <summary>Shared helpers used by several evaluators.</summary>
internal static class Heuristics
{
    /// <summary>Luhn check to reduce false positives when scanning for stored card numbers (PCI DSS Req. 3).</summary>
    public static bool Luhn(string digits)
    {
        int sum = 0; bool alt = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int d = digits[i] - '0';
            if (d < 0 || d > 9) return false;
            if (alt) { d *= 2; if (d > 9) d -= 9; }
            sum += d; alt = !alt;
        }
        return digits.Length is >= 13 and <= 19 && sum % 10 == 0;
    }

    private static readonly Regex CardLike = new(@"(?:\d[ -]?){13,19}", RegexOptions.Compiled);

    /// <summary>Counts Luhn-valid card-number-like sequences across the given string literals.</summary>
    public static int ProbableCardNumbers(IEnumerable<string> literals)
    {
        int count = 0;
        foreach (var s in literals)
        {
            foreach (Match m in CardLike.Matches(s))
            {
                var digits = new string(m.Value.Where(char.IsDigit).ToArray());
                if (Luhn(digits)) count++;
            }
        }
        return count;
    }

    /// <summary>Parse `dotnet test` console output for Passed/Failed/Skipped counts.</summary>
    public static (int passed, int failed, int skipped, bool parsed) ParseDotnetTest(string output)
    {
        int P(string label)
        {
            var m = Regex.Match(output, label + @"!?\s*[:\-]?\s*(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }
        int passed = P("Passed"), failed = P("Failed"), skipped = P("Skipped");
        bool parsed = passed >= 0 || failed >= 0;
        return (Math.Max(0, passed), Math.Max(0, failed), Math.Max(0, skipped), parsed);
    }
}
