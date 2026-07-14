namespace CreditCardApi.Application.CreditCards.Dtos;

public sealed record CreateCreditCardRequest(
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit);
