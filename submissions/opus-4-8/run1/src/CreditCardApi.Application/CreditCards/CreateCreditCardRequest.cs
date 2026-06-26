using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.CreditCards;

/// <summary>Payload to create a credit card.</summary>
public sealed class CreateCreditCardRequest
{
    /// <summary>Name of the cardholder. Required.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string CardholderName { get; init; } = string.Empty;

    /// <summary>The full card number (PAN). Required. Stored encrypted, never returned in clear text.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(19, MinimumLength = 12)]
    [RegularExpression(@"^\d{12,19}$", ErrorMessage = "Card number must be 12 to 19 digits.")]
    public string CardNumber { get; init; } = string.Empty;

    /// <summary>Optional card brand (for example VISA, MASTERCARD).</summary>
    [StringLength(40)]
    public string? Brand { get; init; }

    /// <summary>Approved credit limit. Must be greater than or equal to zero.</summary>
    [Range(0, 9_999_999_999.99, ErrorMessage = "Credit limit must be greater than or equal to zero.")]
    public decimal CreditLimit { get; init; }
}
