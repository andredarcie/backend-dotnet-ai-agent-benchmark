namespace CreditCardApi.Application.CreditCards.Dtos;

/// <summary>
/// <c>CardNumber</c> is always masked (e.g. "**** **** **** 1111") - the API never returns the
/// full PAN, in or out of this response.
/// </summary>
public sealed record CreditCardResponse(
    int Id,
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit,
    DateTime CreatedAt);
