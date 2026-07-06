namespace CreditCardApi.Application.CreditCards;

/// <summary>A credit card as returned by the API.</summary>
/// <param name="Id">Unique identifier of the card.</param>
/// <param name="CardholderName">Name of the cardholder.</param>
/// <param name="CardNumber">Masked card number (e.g. <c>**** **** **** 1234</c>); the full PAN is never returned.</param>
/// <param name="Brand">Card scheme, e.g. <c>VISA</c>. May be <see langword="null"/>.</param>
/// <param name="CreditLimit">Credit limit of the card.</param>
/// <param name="CreatedAt">UTC timestamp assigned by the server on creation.</param>
public sealed record CreditCardResponse(
    int Id,
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit,
    DateTime CreatedAt);
