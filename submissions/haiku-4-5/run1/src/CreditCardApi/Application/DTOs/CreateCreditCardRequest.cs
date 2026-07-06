namespace CreditCardApi.Application.DTOs;

/// <summary>
/// Request to create a new credit card.
/// </summary>
public class CreateCreditCardRequest
{
    /// <summary>
    /// The cardholder name (required, non-empty).
    /// </summary>
    public required string CardholderName { get; set; }

    /// <summary>
    /// The card number (required, non-empty).
    /// </summary>
    public required string CardNumber { get; set; }

    /// <summary>
    /// Card brand (optional).
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// The credit limit (required, >= 0).
    /// </summary>
    public decimal CreditLimit { get; set; }
}
