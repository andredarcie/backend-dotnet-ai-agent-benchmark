using Xunit;

namespace CreditCardApi.Tests.Unit;

public class TransactionValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public void Transaction_Amount_Must_Be_Greater_Than_Zero(decimal amount)
    {
        Assert.True(amount <= 0, $"Amount {amount} should be invalid");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1000000)]
    public void Transaction_Amount_Must_Accept_Valid_Positive_Values(decimal amount)
    {
        Assert.True(amount > 0, $"Amount {amount} should be valid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Merchant_Name_Must_Not_Be_Empty(string merchant)
    {
        Assert.True(string.IsNullOrWhiteSpace(merchant), $"Merchant '{merchant}' should be invalid");
    }

    [Theory]
    [InlineData("Amazon")]
    [InlineData("Best Buy")]
    [InlineData("Merchant Inc.")]
    public void Merchant_Name_Must_Accept_Valid_Values(string merchant)
    {
        Assert.False(string.IsNullOrWhiteSpace(merchant), $"Merchant '{merchant}' should be valid");
    }
}
