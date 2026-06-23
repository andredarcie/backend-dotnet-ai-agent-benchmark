namespace CreditCardApi.Models;

public class CreditCard
{
    public int Id { get; set; }
    public required string CardholderName { get; set; }
    public required string CardNumber { get; set; }
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
