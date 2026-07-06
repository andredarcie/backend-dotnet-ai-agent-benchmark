namespace CreditCardApi.Application.Transactions;

public class TransactionResponse
{
    public int Id { get; init; }

    public int CreditCardId { get; init; }

    public decimal Amount { get; init; }

    public string Merchant { get; init; } = string.Empty;

    public string? Category { get; init; }

    public DateTime CreatedAt { get; init; }
}
