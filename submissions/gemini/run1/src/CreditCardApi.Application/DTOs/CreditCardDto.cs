using System;
using CreditCardApi.Application.Common.Security;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.DTOs;

/// <summary>
/// DTO representing a CreditCard for read operations.
/// </summary>
public class CreditCardDto
{
    public int Id { get; set; }
    public string CardholderName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }

    public static CreditCardDto FromEntity(CreditCard card)
    {
        return new CreditCardDto
        {
            Id = card.Id,
            CardholderName = card.CardholderName,
            CardNumber = CardNumberMasker.Mask(card.CardNumber),
            Brand = card.Brand,
            CreditLimit = card.CreditLimit,
            CreatedAt = card.CreatedAt
        };
    }
}
