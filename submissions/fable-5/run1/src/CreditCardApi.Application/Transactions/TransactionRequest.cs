using System.ComponentModel.DataAnnotations;
using CreditCardApi.Application.Validation;

namespace CreditCardApi.Application.Transactions;

/// <summary>Payload for creating or replacing a transaction.</summary>
public sealed record TransactionRequest
{
    /// <summary>Identifier of the credit card being charged. Required; the card must exist.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int? CreditCardId { get; init; }

    /// <summary>Amount charged. Required, strictly greater than zero.</summary>
    [Required]
    [Range(0d, MonetaryLimits.MaxAmount, MinimumIsExclusive = true)]
    public decimal? Amount { get; init; }

    /// <summary>Merchant where the purchase was made. Required, non-empty.</summary>
    [RequiredNotWhitespace]
    [MaxLength(200)]
    public string? Merchant { get; init; }

    /// <summary>Free-form spending category. Optional.</summary>
    [MaxLength(100)]
    public string? Category { get; init; }
}
