namespace CreditCardApi.Domain.Entities;

/// <summary>A single charge against a <see cref="Entities.CreditCard"/>.</summary>
public class Transaction
{
    private Transaction()
    {
        // Required by EF Core for materialization.
    }

    public Transaction(int creditCardId, decimal amount, string merchant, string? category, DateTime createdAtUtc)
    {
        if (creditCardId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditCardId), creditCardId, "Credit card id must be positive.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(merchant);

        CreditCardId = creditCardId;
        Amount = amount;
        Merchant = merchant;
        Category = category;
        CreatedAt = createdAtUtc;
    }

    public int Id { get; private set; }

    public int CreditCardId { get; private set; }

    public decimal Amount { get; private set; }

    public string Merchant { get; private set; } = string.Empty;

    public string? Category { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public CreditCard? CreditCard { get; private set; }

    public void UpdateDetails(decimal amount, string merchant, string? category)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(merchant);

        Amount = amount;
        Merchant = merchant;
        Category = category;
    }
}
