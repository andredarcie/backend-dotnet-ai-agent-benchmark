namespace CreditCardApi.DTOs;

public class CreateTransactionRequest
{
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public required string Merchant { get; set; }
    public string? Category { get; set; }
}
