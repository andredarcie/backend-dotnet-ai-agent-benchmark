using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Mapping;

/// <summary>Maps domain entities to their outward-facing DTOs. Entities are never exposed directly.</summary>
public static class DtoMappings
{
    /// <summary>Projects a <see cref="CreditCard"/> to its API response, masking the PAN.</summary>
    public static CreditCardResponse ToResponse(this CreditCard card) => new()
    {
        Id = card.Id,
        CardholderName = card.CardholderName,
        CardNumberMasked = MaskPan(card.CardNumberLast4),
        Brand = card.Brand,
        CreditLimit = card.CreditLimit,
        CreatedAt = card.CreatedAt,
    };

    /// <summary>Projects a <see cref="Transaction"/> to its API response.</summary>
    public static TransactionResponse ToResponse(this Transaction transaction) => new()
    {
        Id = transaction.Id,
        CreditCardId = transaction.CreditCardId,
        Amount = transaction.Amount,
        Merchant = transaction.Merchant,
        Category = transaction.Category,
        CreatedAt = transaction.CreatedAt,
    };

    private static string MaskPan(string last4) =>
        string.IsNullOrEmpty(last4) ? "**** **** **** ****" : $"**** **** **** {last4}";
}
