namespace CreditCardApi.DTOs
{
    public class CreateCreditCardRequest
    {
        public string CardholderName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public decimal CreditLimit { get; set; }
    }
}
