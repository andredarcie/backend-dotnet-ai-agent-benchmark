using System.ComponentModel.DataAnnotations;
using CreditCardApi.Application.Validation;

namespace CreditCardApi.Application.CreditCards;

/// <summary>Payload for creating or replacing a credit card.</summary>
public sealed record CreditCardRequest
{
    /// <summary>Name of the cardholder. Required, non-empty.</summary>
    [RequiredNotWhitespace]
    [MaxLength(200)]
    public string? CardholderName { get; init; }

    /// <summary>
    /// Full card number (PAN). Required, non-empty. It is truncated to its last four digits at
    /// the service boundary; responses only ever contain the masked form.
    /// </summary>
    [RequiredNotWhitespace]
    [MaxLength(30)]
    public string? CardNumber { get; init; }

    /// <summary>Card scheme, e.g. <c>VISA</c> or <c>MASTERCARD</c>. Optional.</summary>
    [MaxLength(50)]
    public string? Brand { get; init; }

    /// <summary>Credit limit. Required, zero or greater.</summary>
    [Required]
    [Range(0d, MonetaryLimits.MaxAmount)]
    public decimal? CreditLimit { get; init; }
}
