namespace CreditCardApi.DTOs;

public class UpdateCreditCardRequest
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? Brand { get; set; }
    public decimal? CreditLimit { get; set; }
}
