using CreditCardApi.Application.Common;

namespace CreditCardApi.UnitTests.Common;

public class PanMaskerTests
{
    [Theory]
    [InlineData("4111111111111111", "**** **** **** 1111")]
    [InlineData("4111-1111-1111-1111", "**** **** **** 1111")]
    [InlineData("371449635398431", "**** **** **** 8431")]
    public void Mask_ReturnsGroupsOfAsterisksFollowedByLastFourDigits(string cardNumber, string expected)
    {
        var masked = PanMasker.Mask(cardNumber);

        Assert.Equal(expected, masked);
    }

    [Fact]
    public void Mask_NeverContainsTheOriginalDigitsExceptTheLastFour()
    {
        const string cardNumber = "4111111111111111";

        var masked = PanMasker.Mask(cardNumber);

        Assert.DoesNotContain(cardNumber[..12], masked, StringComparison.Ordinal);
    }

    [Fact]
    public void Mask_WithFewerThanFourDigits_ReturnsAllAsterisks()
    {
        var masked = PanMasker.Mask("123");

        Assert.Equal("***", masked);
    }
}
