namespace CreditCardApi.Application.DTOs;

/// <summary>
/// Request to create a new transaction.
/// </summary>
public class CreateTransactionRequest
{
    /// <summary>
    /// The credit card ID (required, must exist).
    /// </summary>
    public int CreditCardId { get; set; }

    /// <summary>
    /// The transaction amount (required, must be > 0).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The merchant name (required, non-empty).
    /// </summary>
    public required string Merchant { get; set; }

    /// <summary>
    /// The transaction category (optional).
    /// </summary>
    public string? Category { get; set; }
}
