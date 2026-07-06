using CreditCardApi.Domain.Cards;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.CreditCards;

public static class CreditCardMapping
{
    public static CreditCardResponse ToResponse(CreditCard creditCard) => new()
    {
        Id = creditCard.Id,
        CardholderName = creditCard.CardholderName,
        CardNumber = CardNumberPolicy.Mask(creditCard.CardNumberLast4),
        Brand = creditCard.Brand,
        CreditLimit = creditCard.CreditLimit,
        CreatedAt = creditCard.CreatedAt,
    };
}
