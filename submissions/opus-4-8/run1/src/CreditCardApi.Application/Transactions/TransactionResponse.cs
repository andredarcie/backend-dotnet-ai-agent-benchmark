namespace CreditCardApi.Application.Transactions;

/// <summary>API representation of a transaction. This is also the shape published to Kafka.</summary>
public sealed record TransactionResponse
{
    /// <summary>Identifier of the transaction.</summary>
    public int Id { get; init; }

    /// <summary>Identifier of the card that was charged.</summary>
    public int CreditCardId { get; init; }

    /// <summary>Charge amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>Merchant name.</summary>
    public string Merchant { get; init; } = string.Empty;

    /// <summary>Category, if any.</summary>
    public string? Category { get; init; }

    /// <summary>When the transaction was created (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}
