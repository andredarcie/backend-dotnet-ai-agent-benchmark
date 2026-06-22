using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Contracts.CreditCards;

public sealed record CreditCardResponse(
    int Id,
    string CardholderName,
    string CardNumber,
    string? Brand,
    decimal CreditLimit,
    DateTime CreatedAt)
{
    public static CreditCardResponse FromEntity(CreditCard card) =>
        new(card.Id, card.CardholderName, card.CardNumber, card.Brand, card.CreditLimit, card.CreatedAt);
}
