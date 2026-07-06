namespace CreditCardApi.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public required string Merchant { get; set; }
    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public CreditCard? CreditCard { get; set; }
}
