namespace CreditCardApi.Api.Application.Dto;

public class CreateCreditCardRequest
{
    public string CardholderName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
}

public class UpdateCreditCardRequest
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? Brand { get; set; }
    public decimal? CreditLimit { get; set; }
}

public class CreditCardResponse
{
    public int Id { get; set; }
    public string CardholderName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }
}
