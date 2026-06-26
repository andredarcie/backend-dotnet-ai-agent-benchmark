using CreditCardApi.Domain.Exceptions;

namespace CreditCardApi.Domain.Entities;

/// <summary>
/// A single charge against a <see cref="CreditCard"/>. Amounts are always strictly positive.
/// </summary>
public sealed class Transaction
{
    // Parameterless constructor for EF Core materialization.
    private Transaction()
    {
    }

    private Transaction(int creditCardId, decimal amount, string merchant, string? category, DateTime createdAtUtc)
    {
        CreditCardId = creditCardId;
        Amount = amount;
        Merchant = merchant;
        Category = category;
        CreatedAt = createdAtUtc;
    }

    /// <summary>Auto-incremented primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Foreign key to the owning <see cref="CreditCard"/>. Required.</summary>
    public int CreditCardId { get; private set; }

    /// <summary>Navigation to the owning card.</summary>
    public CreditCard? CreditCard { get; private set; }

    /// <summary>Charge amount. Must be strictly greater than zero.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Merchant name. Required, non-empty.</summary>
    public string Merchant { get; private set; } = string.Empty;

    /// <summary>Optional category (for example "shopping", "travel").</summary>
    public string? Category { get; private set; }

    /// <summary>Creation timestamp in UTC, set by the server.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Creates a transaction, enforcing the domain invariants.</summary>
    public static Transaction Create(int creditCardId, decimal amount, string merchant, string? category, DateTime createdAtUtc)
    {
        GuardAmount(amount);
        GuardMerchant(merchant);
        return new Transaction(creditCardId, amount, merchant.Trim(), Normalize(category), createdAtUtc);
    }

    /// <summary>Updates the mutable fields of the transaction, re-validating invariants.</summary>
    public void Update(decimal amount, string merchant, string? category)
    {
        GuardAmount(amount);
        GuardMerchant(merchant);
        Amount = amount;
        Merchant = merchant.Trim();
        Category = Normalize(category);
    }

    private static void GuardAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainValidationException(nameof(amount), "Amount must be greater than zero.");
        }
    }

    private static void GuardMerchant(string merchant)
    {
        if (string.IsNullOrWhiteSpace(merchant))
        {
            throw new DomainValidationException(nameof(merchant), "Merchant is required.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
