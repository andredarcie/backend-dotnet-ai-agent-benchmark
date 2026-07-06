using System.ComponentModel.DataAnnotations;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;

namespace CreditCardApi.UnitTests.Application.Validation;

/// <summary>
/// Verifies the contract rules that are enforced declaratively on the request DTOs
/// (the API returns 400 whenever any of these fail).
/// </summary>
public class RequestValidationTests
{
    [Fact]
    public void CreditCardRequest_Valid_PassesValidation()
    {
        var request = new CreditCardRequest
        {
            CardholderName = "Ada Lovelace",
            CardNumber = "4111111111111111",
            Brand = "VISA",
            CreditLimit = 5000m,
        };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreditCardRequest_BlankCardholderName_Fails(string? name)
    {
        var request = ValidCard() with { CardholderName = name };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(CreditCardRequest.CardholderName)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreditCardRequest_BlankCardNumber_Fails(string? cardNumber)
    {
        var request = ValidCard() with { CardNumber = cardNumber };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(CreditCardRequest.CardNumber)));
    }

    [Fact]
    public void CreditCardRequest_MissingCreditLimit_Fails()
    {
        var request = ValidCard() with { CreditLimit = null };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(CreditCardRequest.CreditLimit)));
    }

    [Fact]
    public void CreditCardRequest_NegativeCreditLimit_Fails()
    {
        var request = ValidCard() with { CreditLimit = -0.01m };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(CreditCardRequest.CreditLimit)));
    }

    [Fact]
    public void CreditCardRequest_ZeroCreditLimit_IsAllowed()
    {
        var request = ValidCard() with { CreditLimit = 0m };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void TransactionRequest_Valid_PassesValidation() =>
        Assert.Empty(Validate(ValidTransaction()));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-199.90)]
    public void TransactionRequest_NonPositiveAmount_Fails(double amount)
    {
        var request = ValidTransaction() with { Amount = (decimal)amount };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(TransactionRequest.Amount)));
    }

    [Fact]
    public void TransactionRequest_MissingAmount_Fails()
    {
        var request = ValidTransaction() with { Amount = null };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(TransactionRequest.Amount)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TransactionRequest_BlankMerchant_Fails(string? merchant)
    {
        var request = ValidTransaction() with { Merchant = merchant };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(TransactionRequest.Merchant)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void TransactionRequest_MissingOrNonPositiveCreditCardId_Fails(int? creditCardId)
    {
        var request = ValidTransaction() with { CreditCardId = creditCardId };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(TransactionRequest.CreditCardId)));
    }

    [Fact]
    public void TransactionRequest_NullCategory_IsAllowed()
    {
        var request = ValidTransaction() with { Category = null };

        Assert.Empty(Validate(request));
    }

    private static CreditCardRequest ValidCard() => new()
    {
        CardholderName = "Ada Lovelace",
        CardNumber = "4111111111111111",
        Brand = "VISA",
        CreditLimit = 5000m,
    };

    private static TransactionRequest ValidTransaction() => new()
    {
        CreditCardId = 1,
        Amount = 199.90m,
        Merchant = "Amazon",
        Category = "shopping",
    };

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
