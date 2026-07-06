using CreditCardApi.Domain.Cards;

namespace CreditCardApi.UnitTests.Domain;

public class CardNumberTests
{
    [Theory]
    [InlineData("4111111111111111", "1111")]
    [InlineData("4111-1111-1111-1234", "1234")]
    [InlineData("5500 0000 0000 0004", "0004")]
    [InlineData("123", "123")]
    public void ToLast4_KeepsOnlyTrailingDigits(string pan, string expected) =>
        Assert.Equal(expected, CardNumber.ToLast4(pan));

    [Fact]
    public void ToLast4_WithoutDigits_FallsBackToTrailingCharacters() =>
        Assert.Equal("efgh", CardNumber.ToLast4("abcd-efgh"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ToLast4_RejectsEmptyInput(string pan) =>
        Assert.ThrowsAny<ArgumentException>(() => CardNumber.ToLast4(pan));

    [Fact]
    public void Mask_FormatsForDisplay() =>
        Assert.Equal("**** **** **** 1234", CardNumber.Mask("1234"));

    [Fact]
    public void Mask_NeverContainsFullPan()
    {
        var masked = CardNumber.Mask(CardNumber.ToLast4("4111111111111111"));

        Assert.DoesNotContain("4111111111111111", masked, StringComparison.Ordinal);
        Assert.EndsWith("1111", masked, StringComparison.Ordinal);
    }
}
