using CreditCardApi.Domain.Entities;

namespace CreditCardApi.UnitTests.Domain;

public class TransactionEntityTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_SetsAllFields()
    {
        var transaction = new Transaction(1, 199.90m, "Amazon", "shopping", CreatedAt);

        Assert.Equal(1, transaction.CreditCardId);
        Assert.Equal(199.90m, transaction.Amount);
        Assert.Equal("Amazon", transaction.Merchant);
        Assert.Equal("shopping", transaction.Category);
        Assert.Equal(CreatedAt, transaction.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveAmount(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transaction(1, amount, "Amazon", null, CreatedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveCreditCardId(int creditCardId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transaction(creditCardId, 10m, "Amazon", null, CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankMerchant(string merchant)
    {
        Assert.Throws<ArgumentException>(() => new Transaction(1, 10m, merchant, null, CreatedAt));
    }

    [Fact]
    public void Constructor_AllowsNullCategory()
    {
        var transaction = new Transaction(1, 10m, "Amazon", null, CreatedAt);
        Assert.Null(transaction.Category);
    }

    [Fact]
    public void UpdateDetails_ReplacesMutableFields()
    {
        var transaction = new Transaction(1, 10m, "Amazon", "shopping", CreatedAt);

        transaction.UpdateDetails(20m, "eBay", "electronics");

        Assert.Equal(20m, transaction.Amount);
        Assert.Equal("eBay", transaction.Merchant);
        Assert.Equal("electronics", transaction.Category);
        Assert.Equal(1, transaction.CreditCardId); // The card a transaction belongs to never changes.
    }

    [Fact]
    public void UpdateDetails_RejectsNonPositiveAmount()
    {
        var transaction = new Transaction(1, 10m, "Amazon", null, CreatedAt);
        Assert.Throws<ArgumentOutOfRangeException>(() => transaction.UpdateDetails(0m, "Amazon", null));
    }
}
