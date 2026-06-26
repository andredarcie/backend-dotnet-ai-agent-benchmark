using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CreditCardApi.UnitTests.Domain;

public sealed class CreditCardTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var card = CreditCard.Create("Ada Lovelace", "cipher", "1234", "VISA", 5000m, Now);

        card.CardholderName.Should().Be("Ada Lovelace");
        card.CardNumberLast4.Should().Be("1234");
        card.CardNumberCiphertext.Should().Be("cipher");
        card.Brand.Should().Be("VISA");
        card.CreditLimit.Should().Be(5000m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_empty_cardholder_throws(string name)
    {
        var act = () => CreditCard.Create(name, "cipher", "1234", null, 1000m, Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Field.Should().Be("cardholderName");
    }

    [Fact]
    public void Create_with_negative_limit_throws()
    {
        var act = () => CreditCard.Create("Ada", "cipher", "1234", null, -1m, Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Field.Should().Be("creditLimit");
    }

    [Fact]
    public void Create_blank_brand_is_normalized_to_null()
    {
        var card = CreditCard.Create("Ada", "cipher", "1234", "   ", 0m, Now);

        card.Brand.Should().BeNull();
    }
}
