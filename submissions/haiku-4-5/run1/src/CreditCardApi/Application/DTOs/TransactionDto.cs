namespace CreditCardApi.Application.DTOs;

public record TransactionDto(
    int Id,
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category,
    DateTime CreatedAt
);

public record CreateTransactionRequest(
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category
);

public record UpdateTransactionRequest(
    decimal? Amount,
    string? Merchant,
    string? Category
);
