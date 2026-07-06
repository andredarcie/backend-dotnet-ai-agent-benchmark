namespace CreditCardApi.Application.DTOs;

/// <summary>
/// Data transfer object for a transaction.
/// </summary>
public class TransactionDto
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The credit card ID.
    /// </summary>
    public int CreditCardId { get; set; }

    /// <summary>
    /// The transaction amount (must be > 0).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The merchant name.
    /// </summary>
    public required string Merchant { get; set; }

    /// <summary>
    /// The transaction category (optional).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// When the transaction was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
