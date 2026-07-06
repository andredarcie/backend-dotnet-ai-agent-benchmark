using System.ComponentModel.DataAnnotations;
using CreditCardApi.Application.Validation;

namespace CreditCardApi.Application.Transactions;

/// <summary>Request body shared by create and update — both require the same fields.</summary>
public class TransactionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "creditCardId must reference an existing credit card.")]
    public int CreditCardId { get; init; }

    [Range(typeof(decimal), MonetaryLimits.MinPositiveAmount, MonetaryLimits.MaxAmount)]
    public decimal Amount { get; init; }

    [RequiredNotWhitespace]
    [StringLength(200)]
    public string Merchant { get; init; } = string.Empty;

    [StringLength(100)]
    public string? Category { get; init; }
}
