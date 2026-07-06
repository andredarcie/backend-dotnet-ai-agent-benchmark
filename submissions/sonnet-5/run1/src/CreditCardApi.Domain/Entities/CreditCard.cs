namespace CreditCardApi.Domain.Entities;

/// <summary>A credit card and its billing history. The PAN itself is never held here — only its last 4 digits.</summary>
public class CreditCard
{
    private readonly List<Transaction> _transactions = [];

    private CreditCard()
    {
        // Required by EF Core for materialization.
    }

    public CreditCard(string cardholderName, string cardNumberLast4, string? brand, decimal creditLimit, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardholderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardNumberLast4);
        if (creditLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditLimit), creditLimit, "Credit limit cannot be negative.");
        }

        CardholderName = cardholderName;
        CardNumberLast4 = cardNumberLast4;
        Brand = brand;
        CreditLimit = creditLimit;
        CreatedAt = createdAtUtc;
    }

    public int Id { get; private set; }

    public string CardholderName { get; private set; } = string.Empty;

    public string CardNumberLast4 { get; private set; } = string.Empty;

    public string? Brand { get; private set; }

    public decimal CreditLimit { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Transaction> Transactions => _transactions;

    public void UpdateDetails(string cardholderName, string cardNumberLast4, string? brand, decimal creditLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardholderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardNumberLast4);
        if (creditLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditLimit), creditLimit, "Credit limit cannot be negative.");
        }

        CardholderName = cardholderName;
        CardNumberLast4 = cardNumberLast4;
        Brand = brand;
        CreditLimit = creditLimit;
    }
}
