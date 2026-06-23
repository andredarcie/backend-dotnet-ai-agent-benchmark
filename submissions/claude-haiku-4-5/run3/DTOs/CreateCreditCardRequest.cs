namespace CreditCardApi.DTOs;

public class CreateCreditCardRequest
{
    public required string CardholderName { get; set; }
    public required string CardNumber { get; set; }
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
}
