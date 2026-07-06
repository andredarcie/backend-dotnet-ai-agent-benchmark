namespace CreditCardApi.Application.DTOs;

/// <summary>
/// Request to update a credit card.
/// </summary>
public class UpdateCreditCardRequest
{
    /// <summary>
    /// The cardholder name (optional).
    /// </summary>
    public string? CardholderName { get; set; }

    /// <summary>
    /// The card number (optional).
    /// </summary>
    public string? CardNumber { get; set; }

    /// <summary>
    /// Card brand (optional).
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// The credit limit (optional).
    /// </summary>
    public decimal? CreditLimit { get; set; }
}
