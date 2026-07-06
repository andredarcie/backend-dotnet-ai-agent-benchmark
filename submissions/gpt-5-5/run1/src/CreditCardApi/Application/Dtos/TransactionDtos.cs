using CreditCardApi.Application.Validation;

namespace CreditCardApi.Application.Dtos;

/// <summary>Request body for creating a transaction.</summary>
public sealed record CreateTransactionRequest(
    int CreditCardId,
    [PositiveDecimal] decimal? Amount,
    [NotBlank] string? Merchant,
    string? Category);

/// <summary>Request body for replacing a transaction.</summary>
public sealed record UpdateTransactionRequest(
    int CreditCardId,
    [PositiveDecimal] decimal? Amount,
    [NotBlank] string? Merchant,
    string? Category);

/// <summary>Transaction representation returned by the API and published to Kafka.</summary>
public sealed record TransactionResponse(
    int Id,
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category,
    DateTimeOffset CreatedAt);

