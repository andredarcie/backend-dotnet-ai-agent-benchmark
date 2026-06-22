namespace CreditCardApi.DTOs;

public record CreateTransactionRequest
{
    public int CreditCardId { get; init; }
    public decimal Amount { get; init; }
    public string? Merchant { get; init; }
    public string? Category { get; init; }
}

public record UpdateTransactionRequest
{
    public int CreditCardId { get; init; }
    public decimal Amount { get; init; }
    public string? Merchant { get; init; }
    public string? Category { get; init; }
}

public record TransactionResponse
{
    public int Id { get; init; }
    public int CreditCardId { get; init; }
    public decimal Amount { get; init; }
    public string Merchant { get; init; } = string.Empty;
    public string? Category { get; init; }
    public DateTime CreatedAt { get; init; }
}
