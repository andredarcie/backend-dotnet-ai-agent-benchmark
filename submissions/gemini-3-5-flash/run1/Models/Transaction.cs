using System;
using System.Text.Json.Serialization;

namespace Gemini.Models;

public class Transaction
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property, ignored in JSON response
    [JsonIgnore]
    public CreditCard? CreditCard { get; set; }
}
