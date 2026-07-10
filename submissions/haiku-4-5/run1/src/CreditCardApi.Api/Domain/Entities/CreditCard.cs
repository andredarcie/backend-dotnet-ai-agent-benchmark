namespace CreditCardApi.Api.Domain.Entities;

public class CreditCard
{
    public int Id { get; set; }
    public string CardholderName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];
}
