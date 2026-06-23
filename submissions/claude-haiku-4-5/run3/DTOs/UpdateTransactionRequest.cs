namespace CreditCardApi.DTOs;

public class UpdateTransactionRequest
{
    public int? CreditCardId { get; set; }
    public decimal? Amount { get; set; }
    public string? Merchant { get; set; }
    public string? Category { get; set; }
}
