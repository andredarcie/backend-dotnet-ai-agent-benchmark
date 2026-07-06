namespace CreditCardApi.Application.DTOs;

/// <summary>
/// Data transfer object for a credit card.
/// </summary>
public class CreditCardDto
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The cardholder name.
    /// </summary>
    public required string CardholderName { get; set; }

    /// <summary>
    /// The masked card number (only last 4 digits shown for security).
    /// </summary>
    public required string CardNumber { get; set; }

    /// <summary>
    /// Card brand (e.g., VISA, MASTERCARD).
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// The credit limit.
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// When the card was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
