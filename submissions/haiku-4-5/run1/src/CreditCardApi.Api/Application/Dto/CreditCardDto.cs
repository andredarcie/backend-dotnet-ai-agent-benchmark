using CreditCardApi.Api.Infrastructure.Security;

namespace CreditCardApi.Api.Application.Dto;

public class CreditCardDto
{
    public int Id { get; set; }
    public string CardholderName { get; set; } = string.Empty;
    public string CardNumberLast4 { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }

    public static CreditCardDto FromEntity(Domain.Entities.CreditCard entity)
    {
        return new CreditCardDto
        {
            Id = entity.Id,
            CardholderName = entity.CardholderName,
            CardNumberLast4 = CardNumberProtection.TruncateCardNumber(entity.CardNumber),
            Brand = entity.Brand,
            CreditLimit = entity.CreditLimit,
            CreatedAt = entity.CreatedAt
        };
    }
}

public class CreateCreditCardRequest
{
    public string CardholderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
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
