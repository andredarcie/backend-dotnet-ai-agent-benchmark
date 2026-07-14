namespace CreditCardApi.Application.Transactions.Dtos;

public sealed record CreateTransactionRequest(
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category);
