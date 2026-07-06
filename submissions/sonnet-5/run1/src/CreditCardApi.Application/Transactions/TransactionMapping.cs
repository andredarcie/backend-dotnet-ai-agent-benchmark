using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Transactions;

public static class TransactionMapping
{
    public static TransactionResponse ToResponse(Transaction transaction) => new()
    {
        Id = transaction.Id,
        CreditCardId = transaction.CreditCardId,
        Amount = transaction.Amount,
        Merchant = transaction.Merchant,
        Category = transaction.Category,
        CreatedAt = transaction.CreatedAt,
    };
}
