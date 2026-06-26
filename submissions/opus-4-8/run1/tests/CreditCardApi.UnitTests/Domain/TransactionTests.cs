using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CreditCardApi.UnitTests.Domain;

public sealed class TransactionTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var transaction = Transaction.Create(1, 199.90m, "Amazon", "shopping", Now);

        transaction.CreditCardId.Should().Be(1);
        transaction.Amount.Should().Be(199.90m);
        transaction.Merchant.Should().Be("Amazon");
        transaction.Category.Should().Be("shopping");
        transaction.CreatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Create_with_non_positive_amount_throws(decimal amount)
    {
        var act = () => Transaction.Create(1, amount, "Amazon", null, Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Field.Should().Be("amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_empty_merchant_throws(string merchant)
    {
        var act = () => Transaction.Create(1, 10m, merchant, null, Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Field.Should().Be("merchant");
    }

    [Fact]
    public void Update_revalidates_invariants()
    {
        var transaction = Transaction.Create(1, 10m, "Amazon", null, Now);

        var act = () => transaction.Update(0m, "Amazon", null);

        act.Should().Throw<DomainValidationException>();
    }
}
