using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class UpdateCreditCardUseCase(ICreditCardRepository creditCardRepository)
{
    public async Task<CreditCard?> ExecuteAsync(int id, UpdateCreditCardDto dto)
    {
        var creditCard = await creditCardRepository.GetByIdAsync(id);
        if (creditCard == null)
            return null;

        if (!string.IsNullOrWhiteSpace(dto.CardholderName))
            creditCard.CardholderName = dto.CardholderName;

        if (!string.IsNullOrWhiteSpace(dto.CardNumber))
            creditCard.CardNumber = dto.CardNumber;

        if (dto.Brand != null)
            creditCard.Brand = dto.Brand;

        if (dto.CreditLimit.HasValue)
            creditCard.CreditLimit = dto.CreditLimit.Value;

        await creditCardRepository.UpdateAsync(creditCard);
        await creditCardRepository.SaveChangesAsync();

        return creditCard;
    }
}
