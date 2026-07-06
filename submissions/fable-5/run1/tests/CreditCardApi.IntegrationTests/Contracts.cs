namespace CreditCardApi.IntegrationTests;

// The tests define the wire contract independently of the application's own DTOs,
// so a accidental contract change in the API surfaces as a test failure here.

/// <summary>Credit card as observed on the wire.</summary>
public sealed record CardDto(
    int Id,
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit,
    DateTime CreatedAt);

/// <summary>Transaction as observed on the wire.</summary>
public sealed record TransactionDto(
    int Id,
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category,
    DateTime CreatedAt);
