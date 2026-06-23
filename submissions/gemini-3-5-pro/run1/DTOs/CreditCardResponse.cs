using System;

namespace CreditCardApi.DTOs
{
    public class CreditCardResponse
    {
        public int Id { get; set; }
        public string CardholderName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public decimal CreditLimit { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
