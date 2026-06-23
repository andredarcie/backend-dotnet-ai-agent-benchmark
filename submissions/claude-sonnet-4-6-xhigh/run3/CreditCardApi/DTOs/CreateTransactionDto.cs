namespace CreditCardApi.DTOs;

public class CreateTransactionDto
{
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string? Category { get; set; }
}
