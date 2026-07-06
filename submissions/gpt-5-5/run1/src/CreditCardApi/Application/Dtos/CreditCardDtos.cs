using CreditCardApi.Application.Validation;

namespace CreditCardApi.Application.Dtos;

/// <summary>Request body for creating a credit card.</summary>
public sealed record CreateCreditCardRequest(
    [NotBlank] string? CardholderName,
    [NotBlank] string? CardNumber,
    string? Brand,
    [NonNegativeDecimal] decimal? CreditLimit);

/// <summary>Request body for replacing a credit card.</summary>
public sealed record UpdateCreditCardRequest(
    [NotBlank] string? CardholderName,
    [NotBlank] string? CardNumber,
    string? Brand,
    [NonNegativeDecimal] decimal? CreditLimit);

/// <summary>Credit card representation returned by the API. The card number is masked.</summary>
public sealed record CreditCardResponse(
    int Id,
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit,
    DateTimeOffset CreatedAt);

