namespace CreditCardApi.DTOs;

public class TransactionResponse
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public required string Merchant { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
}
