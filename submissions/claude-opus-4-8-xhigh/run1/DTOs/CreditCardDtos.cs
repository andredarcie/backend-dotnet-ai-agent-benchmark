namespace CreditCardApi.DTOs;

public record CreateCreditCardRequest
{
    public string? CardholderName { get; init; }
    public string? CardNumber { get; init; }
    public string? Brand { get; init; }
    public decimal CreditLimit { get; init; }
}

public record UpdateCreditCardRequest
{
    public string? CardholderName { get; init; }
    public string? CardNumber { get; init; }
    public string? Brand { get; init; }
    public decimal CreditLimit { get; init; }
}

public record CreditCardResponse
{
    public int Id { get; init; }
    public string CardholderName { get; init; } = string.Empty;
    public string CardNumber { get; init; } = string.Empty;
    public string? Brand { get; init; }
    public decimal CreditLimit { get; init; }
    public DateTime CreatedAt { get; init; }
}
