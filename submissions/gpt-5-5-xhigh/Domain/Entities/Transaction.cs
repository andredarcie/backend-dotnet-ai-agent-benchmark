namespace CreditCardApi.Domain.Entities;

public sealed class Transaction
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public CreditCard CreditCard { get; set; } = null!;
}
