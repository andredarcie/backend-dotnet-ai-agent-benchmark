using BackendEvaluator.Core;
using Xunit;

namespace Evaluator.Tests;

public class ContractOracleTests
{
    [Theory]
    [InlineData("""{"id":1,"cardholderName":"Ada","createdAt":"2026-01-01T00:00:00Z"}""", true)]
    [InlineData("""{"Id":1,"CardholderName":"Ada"}""", false)]       // PascalCase
    [InlineData("""{"credit_limit":100}""", false)]                  // snake_case
    [InlineData("""{"data":{"id":1,"cardNumber":"x"}}""", true)]     // unwrapped envelope, camelCase inside
    [InlineData("[1,2,3]", true)]                                    // not an object -> not penalized
    [InlineData("not json", true)]                                   // unparseable -> not penalized
    public void AllKeysCamelCase_detects_casing(string body, bool expected)
    {
        var (ok, _) = ContractOracle.AllKeysCamelCase(body);
        Assert.Equal(expected, ok);
    }

    [Theory]
    [InlineData("""{"id":42,"merchant":"x"}""", 42)]
    [InlineData("""{"merchant":"x","id":7}""", 7)]
    [InlineData("""{"Id":9}""", 9)]                                  // case-insensitive
    [InlineData("""{"id":"13"}""", 13)]                              // numeric string
    [InlineData("""{"data":{"id":5}}""", 5)]                         // envelope
    [InlineData("""{"merchant":"x"}""", -1)]                         // no id
    [InlineData("garbage", -1)]
    public void TryGetId_extracts_the_new_id(string body, long expected)
    {
        Assert.Equal(expected, ContractOracle.TryGetId(body));
    }

    [Fact]
    public void ExtractCollection_counts_a_bare_array()
    {
        var (count, hasMeta) = ContractOracle.ExtractCollection("[{},{}]");
        Assert.Equal(2, count);
        Assert.False(hasMeta);
    }

    [Fact]
    public void ExtractCollection_reads_an_envelope_with_paging_metadata()
    {
        var (count, hasMeta) = ContractOracle.ExtractCollection("""{"items":[{}],"pageNumber":1,"pageSize":1,"totalCount":5}""");
        Assert.Equal(1, count);
        Assert.True(hasMeta);
    }

    [Fact]
    public void ExtractCollection_handles_non_collections()
    {
        var (count, hasMeta) = ContractOracle.ExtractCollection("""{"id":1}""");
        Assert.Null(count);
        Assert.False(hasMeta);
    }
}
