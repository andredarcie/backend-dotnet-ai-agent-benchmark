using CreditCardApi.Contracts.CreditCards;
using CreditCardApi.Contracts.Transactions;

namespace CreditCardApi.Application.Common;

public static class Validation
{
    public static IReadOnlyList<string> Validate(CreditCardRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CardholderName))
            errors.Add("cardholderName is required.");

        if (string.IsNullOrWhiteSpace(request.CardNumber))
            errors.Add("cardNumber is required.");

        if (request.CreditLimit < 0)
            errors.Add("creditLimit must be greater than or equal to zero.");

        return errors;
    }

    public static IReadOnlyList<string> Validate(TransactionRequest request)
    {
        var errors = new List<string>();

        if (request.CreditCardId <= 0)
            errors.Add("creditCardId must reference an existing credit card.");

        if (request.Amount <= 0)
            errors.Add("amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.Merchant))
            errors.Add("merchant is required.");

        return errors;
    }
}
