using CreditCardApi.Domain.Exceptions;

namespace CreditCardApi.Domain.Entities;

/// <summary>
/// A credit card. The Primary Account Number (PAN) is never stored or exposed in clear text:
/// the entity holds only the ciphertext and the last four digits used for display.
/// </summary>
public sealed class CreditCard
{
    private readonly List<Transaction> _transactions = [];

    // Parameterless constructor for EF Core materialization.
    private CreditCard()
    {
    }

    private CreditCard(
        string cardholderName,
        string cardNumberCiphertext,
        string cardNumberLast4,
        string? brand,
        decimal creditLimit,
        DateTime createdAtUtc)
    {
        CardholderName = cardholderName;
        CardNumberCiphertext = cardNumberCiphertext;
        CardNumberLast4 = cardNumberLast4;
        Brand = brand;
        CreditLimit = creditLimit;
        CreatedAt = createdAtUtc;
    }

    /// <summary>Auto-incremented primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Name of the cardholder. Required, non-empty.</summary>
    public string CardholderName { get; private set; } = string.Empty;

    /// <summary>Encrypted PAN (ciphertext). Never logged, never returned to clients.</summary>
    public string CardNumberCiphertext { get; private set; } = string.Empty;

    /// <summary>Last four digits of the PAN, retained for display (e.g. masked rendering).</summary>
    public string CardNumberLast4 { get; private set; } = string.Empty;

    /// <summary>Optional card brand (for example VISA, MASTERCARD).</summary>
    public string? Brand { get; private set; }

    /// <summary>Approved credit limit. Must be greater than or equal to zero.</summary>
    public decimal CreditLimit { get; private set; }

    /// <summary>Creation timestamp in UTC, set by the server.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Transactions charged to this card (one-to-many).</summary>
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    /// <summary>
    /// Creates a new credit card, enforcing the domain invariants. The PAN must already be
    /// protected by the caller (ciphertext + last four), so clear-text never reaches the domain store.
    /// </summary>
    public static CreditCard Create(
        string cardholderName,
        string cardNumberCiphertext,
        string cardNumberLast4,
        string? brand,
        decimal creditLimit,
        DateTime createdAtUtc)
    {
        GuardCardholderName(cardholderName);
        GuardCreditLimit(creditLimit);
        if (string.IsNullOrWhiteSpace(cardNumberCiphertext))
        {
            throw new DomainValidationException(nameof(cardNumberCiphertext), "Protected card number is required.");
        }

        return new CreditCard(cardholderName.Trim(), cardNumberCiphertext, cardNumberLast4, Normalize(brand), creditLimit, createdAtUtc);
    }

    /// <summary>Updates the mutable fields of the card, re-validating invariants.</summary>
    public void Update(string cardholderName, string? brand, decimal creditLimit)
    {
        GuardCardholderName(cardholderName);
        GuardCreditLimit(creditLimit);
        CardholderName = cardholderName.Trim();
        Brand = Normalize(brand);
        CreditLimit = creditLimit;
    }

    private static void GuardCardholderName(string cardholderName)
    {
        if (string.IsNullOrWhiteSpace(cardholderName))
        {
            throw new DomainValidationException(nameof(cardholderName), "Cardholder name is required.");
        }
    }

    private static void GuardCreditLimit(decimal creditLimit)
    {
        if (creditLimit < 0)
        {
            throw new DomainValidationException(nameof(creditLimit), "Credit limit must be greater than or equal to zero.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
