using CreditCardApi.Domain.Cards;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.CreditCards;

internal static class CreditCardMapping
{
    public static CreditCardResponse ToResponse(this CreditCard card) =>
        new(
            card.Id,
            card.CardholderName,
            CardNumber.Mask(card.CardNumberLast4),
            card.Brand,
            card.CreditLimit,
            card.CreatedAt);
}
