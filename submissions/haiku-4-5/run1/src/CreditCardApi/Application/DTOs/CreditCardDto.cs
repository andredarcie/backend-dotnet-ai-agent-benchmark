namespace CreditCardApi.Application.DTOs;

public record CreditCardDto(
    int Id,
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit,
    DateTime CreatedAt
);

public record CreateCreditCardRequest(
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit
);

public record UpdateCreditCardRequest(
    string? CardholderName,
    string? CardNumber,
    string? Brand,
    decimal? CreditLimit
);
