namespace CreditCardApi.Application.CreditCards;

/// <summary>API representation of a credit card. The PAN is shown masked, never in clear text.</summary>
public sealed record CreditCardResponse
{
    /// <summary>Identifier of the card.</summary>
    public int Id { get; init; }

    /// <summary>Name of the cardholder.</summary>
    public string CardholderName { get; init; } = string.Empty;

    /// <summary>The masked card number, for example <c>**** **** **** 1234</c>.</summary>
    public string CardNumberMasked { get; init; } = string.Empty;

    /// <summary>Card brand, if known.</summary>
    public string? Brand { get; init; }

    /// <summary>Approved credit limit.</summary>
    public decimal CreditLimit { get; init; }

    /// <summary>When the card was created (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}
