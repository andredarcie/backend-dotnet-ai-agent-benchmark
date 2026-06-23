using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class CreateCreditCardUseCase(ICreditCardRepository creditCardRepository)
{
    public async Task<CreditCard> ExecuteAsync(CreateCreditCardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardholderName))
            throw new ArgumentException("CardholderName is required");

        if (string.IsNullOrWhiteSpace(dto.CardNumber))
            throw new ArgumentException("CardNumber is required");

        var creditCard = new CreditCard
        {
            CardholderName = dto.CardholderName,
            CardNumber = dto.CardNumber,
            Brand = dto.Brand,
            CreditLimit = dto.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        await creditCardRepository.AddAsync(creditCard);
        await creditCardRepository.SaveChangesAsync();

        return creditCard;
    }
}
