using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Transactions;

internal static class TransactionMapping
{
    public static TransactionResponse ToResponse(this Transaction transaction) =>
        new(
            transaction.Id,
            transaction.CreditCardId,
            transaction.Amount,
            transaction.Merchant,
            transaction.Category,
            transaction.CreatedAt);
}
