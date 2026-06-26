using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.Transactions;

/// <summary>Payload to update a transaction. The owning card cannot be changed.</summary>
public sealed class UpdateTransactionRequest
{
    /// <summary>Charge amount. Must be greater than zero.</summary>
    [Range(0.01, 9_999_999_999.99, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; init; }

    /// <summary>Merchant name. Required.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Merchant { get; init; } = string.Empty;

    /// <summary>Optional category (for example "shopping").</summary>
    [StringLength(80)]
    public string? Category { get; init; }
}
