using System;
using System.Collections.Generic;

namespace CreditCardApi.Domain.Entities;

/// <summary>
/// Domain entity representing a Credit Card.
/// </summary>
public class CreditCard
{
    private string _cardholderName = null!;
    private string _cardNumber = null!;
    private decimal _creditLimit;

    public int Id { get; set; }

    public string CardholderName
    {
        get => _cardholderName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Cardholder name is required.", nameof(value));
            _cardholderName = value;
        }
    }

    public string CardNumber
    {
        get => _cardNumber;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Card number is required.", nameof(value));
            _cardNumber = value;
        }
    }

    public string? Brand { get; set; }

    public decimal CreditLimit
    {
        get => _creditLimit;
        set
        {
            if (value < 0)
                throw new ArgumentException("Credit limit must be greater than or equal to 0.", nameof(value));
            _creditLimit = value;
        }
    }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control (using PostgreSQL xmin-based system column or similar).
    /// </summary>
    public uint RowVersion { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // Parameterless constructor for EF Core
    protected CreditCard() { }

    public CreditCard(string cardholderName, string cardNumber, string? brand, decimal creditLimit)
    {
        CardholderName = cardholderName;
        CardNumber = cardNumber;
        Brand = brand;
        CreditLimit = creditLimit;
        CreatedAt = DateTime.UtcNow;
    }
}
