using System;
using System.Text.Json.Serialization;

namespace CreditCardApi.Domain
{
    public class Transaction
    {
        public int Id { get; set; }
        public int CreditCardId { get; set; }
        public decimal Amount { get; set; }
        public string Merchant { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        public CreditCard? CreditCard { get; set; }
    }
}
