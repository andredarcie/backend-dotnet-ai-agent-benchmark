using System.ComponentModel.DataAnnotations;
using CreditCardApi.Application.Validation;

namespace CreditCardApi.Application.CreditCards;

/// <summary>Request body shared by create and update — both require the same fields.</summary>
public class CreditCardRequest
{
    [RequiredNotWhitespace]
    [StringLength(200)]
    public string CardholderName { get; init; } = string.Empty;

    /// <summary>The full PAN as submitted by the client. Never persisted or echoed back — see <c>CardNumberPolicy</c>.</summary>
    [RequiredNotWhitespace]
    [StringLength(30, MinimumLength = 4)]
    public string CardNumber { get; init; } = string.Empty;

    [StringLength(30)]
    public string? Brand { get; init; }

    [Range(typeof(decimal), "0", MonetaryLimits.MaxAmount)]
    public decimal CreditLimit { get; init; }
}
