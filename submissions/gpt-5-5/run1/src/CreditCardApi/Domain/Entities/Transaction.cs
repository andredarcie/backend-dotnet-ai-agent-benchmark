using CreditCardApi.Domain.Common;

namespace CreditCardApi.Domain.Entities;

public sealed class Transaction : IConcurrencyTracked
{
    private Transaction()
    {
    }

    public Transaction(int creditCardId, decimal amount, string merchant, string? category, DateTimeOffset createdAt)
    {
        SetCreditCardId(creditCardId);
        SetAmount(amount);
        SetMerchant(merchant);
        SetCategory(category);
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public int CreditCardId { get; private set; }

    public CreditCard? CreditCard { get; private set; }

    public decimal Amount { get; private set; }

    public string Merchant { get; private set; } = string.Empty;

    public string? Category { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int Version { get; private set; } = 1;

    public void Update(int creditCardId, decimal amount, string merchant, string? category)
    {
        SetCreditCardId(creditCardId);
        SetAmount(amount);
        SetMerchant(merchant);
        SetCategory(category);
    }

    public void IncrementVersion() => Version++;

    private void SetCreditCardId(int creditCardId)
    {
        if (creditCardId <= 0)
        {
            throw new DomainRuleException("Credit card id is required.");
        }

        CreditCardId = creditCardId;
    }

    private void SetAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainRuleException("Amount must be greater than zero.");
        }

        Amount = amount;
    }

    private void SetMerchant(string merchant)
    {
        if (string.IsNullOrWhiteSpace(merchant))
        {
            throw new DomainRuleException("Merchant is required.");
        }

        Merchant = merchant.Trim();
    }

    private void SetCategory(string? category)
    {
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
    }
}
