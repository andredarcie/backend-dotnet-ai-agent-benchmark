namespace CreditCardApi.Api.Application.Dto;

public class CreateTransactionRequest
{
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = null!;
    public string? Category { get; set; }
}

public class UpdateTransactionRequest
{
    public decimal? Amount { get; set; }
    public string? Merchant { get; set; }
    public string? Category { get; set; }
}

public class TransactionResponse
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = null!;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
}
