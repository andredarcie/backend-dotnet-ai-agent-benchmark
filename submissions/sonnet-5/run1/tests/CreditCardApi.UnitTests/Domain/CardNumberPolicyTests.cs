using CreditCardApi.Domain.Cards;

namespace CreditCardApi.UnitTests.Domain;

public class CardNumberPolicyTests
{
    [Theory]
    [InlineData("4111111111111111", "1111")]
    [InlineData("4111 1111 1111 1111", "1111")]
    [InlineData("4111-1111-1111-1111", "1111")]
    [InlineData("1234", "1234")]
    public void TruncateToLast4_KeepsOnlyTheLastFourDigits(string rawCardNumber, string expected)
    {
        Assert.Equal(expected, CardNumberPolicy.TruncateToLast4(rawCardNumber));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void TruncateToLast4_RejectsFewerThanFourDigits(string rawCardNumber)
    {
        Assert.ThrowsAny<ArgumentException>(() => CardNumberPolicy.TruncateToLast4(rawCardNumber));
    }

    [Fact]
    public void TruncateToLast4_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => CardNumberPolicy.TruncateToLast4(null!));
    }

    [Fact]
    public void Mask_FormatsAsFourGroupsWithOnlyTheLastVisible()
    {
        Assert.Equal("**** **** **** 1234", CardNumberPolicy.Mask("1234"));
    }
}
