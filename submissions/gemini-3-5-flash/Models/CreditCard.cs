using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gemini.Models;

public class CreditCard
{
    public int Id { get; set; }
    public string CardholderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property for 1:N relationship, ignored in JSON response
    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
