using System.ComponentModel.DataAnnotations;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;

namespace CreditCardApi.UnitTests.Application.Validation;

public class RequestValidationTests
{
    private static bool IsValid(object request, out List<ValidationResult> results)
    {
        results = [];
        return Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
    }

    [Fact]
    public void CreditCardRequest_ValidWhenAllRulesSatisfied()
    {
        var request = new CreditCardRequest
        {
            CardholderName = "Ada Lovelace",
            CardNumber = "4111111111111111",
            Brand = "VISA",
            CreditLimit = 5000m,
        };

        Assert.True(IsValid(request, out _));
    }

    [Fact]
    public void CreditCardRequest_InvalidWhenCardholderNameIsBlank()
    {
        var request = new CreditCardRequest { CardholderName = "  ", CardNumber = "4111111111111111", CreditLimit = 100m };

        Assert.False(IsValid(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreditCardRequest.CardholderName)));
    }

    [Fact]
    public void CreditCardRequest_InvalidWhenCardNumberIsBlank()
    {
        var request = new CreditCardRequest { CardholderName = "Ada Lovelace", CardNumber = "", CreditLimit = 100m };

        Assert.False(IsValid(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreditCardRequest.CardNumber)));
    }

    [Fact]
    public void CreditCardRequest_InvalidWhenCreditLimitIsNegative()
    {
        var request = new CreditCardRequest { CardholderName = "Ada Lovelace", CardNumber = "4111111111111111", CreditLimit = -1m };

        Assert.False(IsValid(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreditCardRequest.CreditLimit)));
    }

    [Fact]
    public void CreditCardRequest_ValidWhenCreditLimitIsZero()
    {
        var request = new CreditCardRequest { CardholderName = "Ada Lovelace", CardNumber = "4111111111111111", CreditLimit = 0m };
        Assert.True(IsValid(request, out _));
    }

    [Fact]
    public void TransactionRequest_ValidWhenAllRulesSatisfied()
    {
        var request = new TransactionRequest { CreditCardId = 1, Amount = 10.5m, Merchant = "Amazon", Category = "shopping" };
        Assert.True(IsValid(request, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TransactionRequest_InvalidWhenAmountIsNotPositive(decimal amount)
    {
        var request = new TransactionRequest { CreditCardId = 1, Amount = amount, Merchant = "Amazon" };

        Assert.False(IsValid(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TransactionRequest.Amount)));
    }

    [Fact]
    public void TransactionRequest_InvalidWhenMerchantIsBlank()
    {
        var request = new TransactionRequest { CreditCardId = 1, Amount = 10m, Merchant = " " };

        Assert.False(IsValid(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TransactionRequest.Merchant)));
    }

    [Fact]
    public void TransactionRequest_InvalidWhenCreditCardIdIsNotPositive()
    {
        var request = new TransactionRequest { CreditCardId = 0, Amount = 10m, Merchant = "Amazon" };

        Assert.False(IsValid(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TransactionRequest.CreditCardId)));
    }
}
