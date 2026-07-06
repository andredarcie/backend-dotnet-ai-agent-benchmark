using CreditCardApi.Domain.Entities;

namespace CreditCardApi.UnitTests.Domain;

public class CreditCardEntityTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_SetsAllFields()
    {
        var card = new CreditCard("Ada Lovelace", "1234", "VISA", 5000m, CreatedAt);

        Assert.Equal("Ada Lovelace", card.CardholderName);
        Assert.Equal("1234", card.CardNumberLast4);
        Assert.Equal("VISA", card.Brand);
        Assert.Equal(5000m, card.CreditLimit);
        Assert.Equal(CreatedAt, card.CreatedAt);
        Assert.Empty(card.Transactions);
    }

    [Fact]
    public void Constructor_AllowsNullBrand()
    {
        var card = new CreditCard("Ada Lovelace", "1234", null, 5000m, CreatedAt);
        Assert.Null(card.Brand);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankCardholderName(string cardholderName)
    {
        Assert.Throws<ArgumentException>(() => new CreditCard(cardholderName, "1234", "VISA", 5000m, CreatedAt));
    }

    [Fact]
    public void Constructor_RejectsNegativeCreditLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditCard("Ada Lovelace", "1234", "VISA", -1m, CreatedAt));
    }

    [Fact]
    public void Constructor_AllowsZeroCreditLimit()
    {
        var card = new CreditCard("Ada Lovelace", "1234", "VISA", 0m, CreatedAt);
        Assert.Equal(0m, card.CreditLimit);
    }

    [Fact]
    public void UpdateDetails_ReplacesMutableFields()
    {
        var card = new CreditCard("Ada Lovelace", "1234", "VISA", 5000m, CreatedAt);

        card.UpdateDetails("Ada L.", "5678", "MASTERCARD", 7500m);

        Assert.Equal("Ada L.", card.CardholderName);
        Assert.Equal("5678", card.CardNumberLast4);
        Assert.Equal("MASTERCARD", card.Brand);
        Assert.Equal(7500m, card.CreditLimit);
        Assert.Equal(CreatedAt, card.CreatedAt); // CreatedAt never changes.
    }

    [Fact]
    public void UpdateDetails_RejectsNegativeCreditLimit()
    {
        var card = new CreditCard("Ada Lovelace", "1234", "VISA", 5000m, CreatedAt);
        Assert.Throws<ArgumentOutOfRangeException>(() => card.UpdateDetails("Ada Lovelace", "1234", "VISA", -1m));
    }
}
