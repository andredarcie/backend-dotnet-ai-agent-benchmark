using CreditCardApi.Domain.Common;

namespace CreditCardApi.Domain.Entities;

public sealed class CreditCard : IConcurrencyTracked
{
    private readonly List<Transaction> _transactions = [];

    private CreditCard()
    {
    }

    public CreditCard(
        string cardholderName,
        string cardNumberCipherText,
        string cardNumberLast4,
        string? brand,
        decimal creditLimit,
        DateTimeOffset createdAt)
    {
        SetCardholderName(cardholderName);
        SetProtectedCardNumber(cardNumberCipherText, cardNumberLast4);
        SetBrand(brand);
        SetCreditLimit(creditLimit);
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string CardholderName { get; private set; } = string.Empty;

    public string CardNumberCipherText { get; private set; } = string.Empty;

    public string CardNumberLast4 { get; private set; } = string.Empty;

    public string? Brand { get; private set; }

    public decimal CreditLimit { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int Version { get; private set; } = 1;

    public IReadOnlyCollection<Transaction> Transactions => _transactions;

    public void Update(
        string cardholderName,
        string cardNumberCipherText,
        string cardNumberLast4,
        string? brand,
        decimal creditLimit)
    {
        SetCardholderName(cardholderName);
        SetProtectedCardNumber(cardNumberCipherText, cardNumberLast4);
        SetBrand(brand);
        SetCreditLimit(creditLimit);
    }

    public void IncrementVersion() => Version++;

    private void SetCardholderName(string cardholderName)
    {
        if (string.IsNullOrWhiteSpace(cardholderName))
        {
            throw new DomainRuleException("Cardholder name is required.");
        }

        CardholderName = cardholderName.Trim();
    }

    private void SetProtectedCardNumber(string cardNumberCipherText, string cardNumberLast4)
    {
        if (string.IsNullOrWhiteSpace(cardNumberCipherText))
        {
            throw new DomainRuleException("Protected card number is required.");
        }

        if (string.IsNullOrWhiteSpace(cardNumberLast4))
        {
            throw new DomainRuleException("Card number fingerprint is required.");
        }

        CardNumberCipherText = cardNumberCipherText;
        CardNumberLast4 = cardNumberLast4.Trim();
    }

    private void SetBrand(string? brand)
    {
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim().ToUpperInvariant();
    }

    private void SetCreditLimit(decimal creditLimit)
    {
        if (creditLimit < 0)
        {
            throw new DomainRuleException("Credit limit must be greater than or equal to zero.");
        }

        CreditLimit = creditLimit;
    }
}
