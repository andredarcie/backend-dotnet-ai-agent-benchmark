namespace CreditCardApi.Application.DTOs;

/// <summary>
/// Request to update a transaction.
/// </summary>
public class UpdateTransactionRequest
{
    /// <summary>
    /// The credit card ID (optional).
    /// </summary>
    public int? CreditCardId { get; set; }

    /// <summary>
    /// The transaction amount (optional).
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// The merchant name (optional).
    /// </summary>
    public string? Merchant { get; set; }

    /// <summary>
    /// The transaction category (optional).
    /// </summary>
    public string? Category { get; set; }
}
