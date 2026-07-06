namespace CreditCardApi.Application.CreditCards;

public class CreditCardResponse
{
    public int Id { get; init; }

    public string CardholderName { get; init; } = string.Empty;

    /// <summary>Masked — e.g. <c>**** **** **** 1234</c>. The full PAN is never stored, so it can never be returned.</summary>
    public string CardNumber { get; init; } = string.Empty;

    public string? Brand { get; init; }

    public decimal CreditLimit { get; init; }

    public DateTime CreatedAt { get; init; }
}
