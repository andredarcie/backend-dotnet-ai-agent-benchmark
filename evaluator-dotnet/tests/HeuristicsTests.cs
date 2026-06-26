using BackendEvaluator.Evaluators;
using Xunit;

namespace Evaluator.Tests;

public class HeuristicsTests
{
    [Theory]
    [InlineData("4111111111111111", true)]   // Visa test PAN (Luhn-valid)
    [InlineData("4242424242424242", true)]   // Stripe test Visa
    [InlineData("5555555555554444", true)]   // Mastercard test PAN
    [InlineData("378282246310005", true)]    // Amex test PAN (15 digits)
    [InlineData("4111111111111112", false)]  // checksum off by one
    [InlineData("1234567890123456", false)]  // not Luhn-valid
    public void Luhn_validates_card_checksums(string digits, bool expected)
        => Assert.Equal(expected, Heuristics.Luhn(digits));

    [Theory]
    [InlineData("123456", false)]                 // too short (<13)
    [InlineData("41111111111111111111", false)]   // too long (>19)
    public void Luhn_rejects_out_of_range_lengths(string digits, bool expected)
        => Assert.Equal(expected, Heuristics.Luhn(digits));

    [Fact]
    public void ProbableCardNumbers_counts_luhn_valid_pans_with_separators()
    {
        var literals = new[]
        {
            "card = 4111 1111 1111 1111",     // spaced -> 1
            "pan:4242-4242-4242-4242",        // dashed -> 1
            "order id 1234567890123456",      // not Luhn -> 0
            "phone 555-0100",                 // too short -> 0
        };
        Assert.Equal(2, Heuristics.ProbableCardNumbers(literals));
    }

    [Fact]
    public void ProbableCardNumbers_is_zero_for_clean_literals()
        => Assert.Equal(0, Heuristics.ProbableCardNumbers(new[] { "hello", "GET /api/cards", "timeout=30" }));

    [Fact]
    public void ParseDotnetTest_reads_a_passing_summary()
    {
        var (passed, failed, skipped, parsed) =
            Heuristics.ParseDotnetTest("Passed!  - Failed:     0, Passed:    12, Skipped:     1, Total:    13");
        Assert.True(parsed);
        Assert.Equal(12, passed);
        Assert.Equal(0, failed);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void ParseDotnetTest_reads_a_failing_summary()
    {
        var (passed, failed, _, parsed) =
            Heuristics.ParseDotnetTest("Failed!  - Failed:     2, Passed:    10, Skipped:     0, Total:    12");
        Assert.True(parsed);
        Assert.Equal(10, passed);
        Assert.Equal(2, failed);
    }

    [Fact]
    public void ParseDotnetTest_reports_unparseable_output()
    {
        var (_, _, _, parsed) = Heuristics.ParseDotnetTest("No test is available in the project.");
        Assert.False(parsed);
    }
}
