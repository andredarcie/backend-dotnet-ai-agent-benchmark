using System;

namespace CreditCardApi.Domain.Entities;

/// <summary>
/// Domain entity representing a transaction against a Credit Card.
/// </summary>
public class Transaction
{
    private decimal _amount;
    private string _merchant = null!;

    public int Id { get; set; }

    public int CreditCardId { get; set; }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (value <= 0)
                throw new ArgumentException("Amount must be greater than 0.", nameof(value));
            _amount = value;
        }
    }

    public string Merchant
    {
        get => _merchant;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Merchant name is required.", nameof(value));
            _merchant = value;
        }
    }

    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; }

    public CreditCard? CreditCard { get; set; }

    // Parameterless constructor for EF Core
    protected Transaction() { }

    public Transaction(int creditCardId, decimal amount, string merchant, string? category)
    {
        CreditCardId = creditCardId;
        Amount = amount;
        Merchant = merchant;
        Category = category;
        CreatedAt = DateTime.UtcNow;
    }
}
