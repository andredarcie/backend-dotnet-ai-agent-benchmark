namespace CreditCardApi.Contracts.CreditCards;

public sealed class CreditCardRequest
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
}
