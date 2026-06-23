using CreditCardApi.DTOs;
using CreditCardApi.Models;

namespace CreditCardApi.Application;

/// <summary>
/// Maps domain entities to outward-facing response DTOs so navigation properties
/// never leak into the API surface (and reference cycles never get serialized).
/// </summary>
public static class EntityMapper
{
    public static CreditCardResponse ToResponse(this CreditCard card) => new()
    {
        Id = card.Id,
        CardholderName = card.CardholderName,
        CardNumber = card.CardNumber,
        Brand = card.Brand,
        CreditLimit = card.CreditLimit,
        CreatedAt = card.CreatedAt
    };

    public static TransactionResponse ToResponse(this Transaction transaction) => new()
    {
        Id = transaction.Id,
        CreditCardId = transaction.CreditCardId,
        Amount = transaction.Amount,
        Merchant = transaction.Merchant,
        Category = transaction.Category,
        CreatedAt = transaction.CreatedAt
    };
}
