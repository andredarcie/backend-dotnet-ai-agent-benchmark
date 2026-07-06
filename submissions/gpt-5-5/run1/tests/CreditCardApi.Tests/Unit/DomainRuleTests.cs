using CreditCardApi.Domain.Common;
using CreditCardApi.Domain.Entities;
using FluentAssertions;

namespace CreditCardApi.Tests.Unit;

public sealed class DomainRuleTests
{
    [Fact]
    public void CreditCard_rejects_blank_cardholder_name()
    {
        var act = () => new CreditCard(" ", "cipher", "1111", "visa", 1000m, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleException>()
            .WithMessage("Cardholder name is required.");
    }

    [Fact]
    public void CreditCard_rejects_negative_credit_limit()
    {
        var act = () => new CreditCard("Ada Lovelace", "cipher", "1111", "visa", -1m, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleException>()
            .WithMessage("Credit limit must be greater than or equal to zero.");
    }

    [Fact]
    public void Transaction_rejects_non_positive_amount()
    {
        var act = () => new Transaction(1, 0m, "Amazon", "shopping", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleException>()
            .WithMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void Transaction_trims_required_text_fields()
    {
        var transaction = new Transaction(1, 10m, "  Amazon  ", "  shopping  ", DateTimeOffset.UtcNow);

        transaction.Merchant.Should().Be("Amazon");
        transaction.Category.Should().Be("shopping");
    }
}
