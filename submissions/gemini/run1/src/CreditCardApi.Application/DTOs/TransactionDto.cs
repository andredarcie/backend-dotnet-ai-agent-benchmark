using System;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.DTOs;

/// <summary>
/// DTO representing a Transaction for read operations.
/// </summary>
public class TransactionDto
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = null!;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }

    public static TransactionDto FromEntity(Transaction txn)
    {
        return new TransactionDto
        {
            Id = txn.Id,
            CreditCardId = txn.CreditCardId,
            Amount = txn.Amount,
            Merchant = txn.Merchant,
            Category = txn.Category,
            CreatedAt = txn.CreatedAt
        };
    }
}
