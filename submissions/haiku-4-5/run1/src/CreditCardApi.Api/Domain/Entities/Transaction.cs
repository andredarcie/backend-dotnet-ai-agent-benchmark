namespace CreditCardApi.Api.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = null!;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = null!;

    public CreditCard CreditCard { get; set; } = null!;
}
