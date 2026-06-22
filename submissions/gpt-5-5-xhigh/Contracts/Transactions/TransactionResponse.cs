using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Contracts.Transactions;

public sealed record TransactionResponse(
    int Id,
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category,
    DateTime CreatedAt)
{
    public static TransactionResponse FromEntity(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.CreditCardId,
            transaction.Amount,
            transaction.Merchant,
            transaction.Category,
            transaction.CreatedAt);
}
