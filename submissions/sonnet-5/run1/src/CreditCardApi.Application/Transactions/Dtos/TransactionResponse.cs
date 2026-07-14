namespace CreditCardApi.Application.Transactions.Dtos;

public sealed record TransactionResponse(
    int Id,
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category,
    DateTime CreatedAt);
