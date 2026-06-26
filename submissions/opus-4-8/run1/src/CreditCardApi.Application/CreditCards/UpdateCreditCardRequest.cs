using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.CreditCards;

/// <summary>Payload to update a credit card. The card number (PAN) is immutable and cannot be changed.</summary>
public sealed class UpdateCreditCardRequest
{
    /// <summary>Name of the cardholder. Required.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string CardholderName { get; init; } = string.Empty;

    /// <summary>Optional card brand (for example VISA, MASTERCARD).</summary>
    [StringLength(40)]
    public string? Brand { get; init; }

    /// <summary>Approved credit limit. Must be greater than or equal to zero.</summary>
    [Range(0, 9_999_999_999.99, ErrorMessage = "Credit limit must be greater than or equal to zero.")]
    public decimal CreditLimit { get; init; }
}
