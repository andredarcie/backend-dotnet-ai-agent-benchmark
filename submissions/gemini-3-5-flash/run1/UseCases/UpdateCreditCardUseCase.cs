using System;
using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

public class UpdateCreditCardUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;

    public UpdateCreditCardUseCase(ICreditCardRepository creditCardRepository)
    {
        _creditCardRepository = creditCardRepository;
    }

    public async Task<CreditCard?> ExecuteAsync(int id, CreditCard updatedCard)
    {
        var existingCard = await _creditCardRepository.GetByIdAsync(id);
        if (existingCard == null)
        {
            return null;
        }

        // Validation rules
        if (string.IsNullOrWhiteSpace(updatedCard.CardholderName) || string.IsNullOrWhiteSpace(updatedCard.CardNumber))
        {
            throw new ArgumentException("CardholderName and CardNumber are required.");
        }

        if (updatedCard.CreditLimit < 0)
        {
            throw new ArgumentException("CreditLimit must be greater than or equal to 0.");
        }

        existingCard.CardholderName = updatedCard.CardholderName;
        existingCard.CardNumber = updatedCard.CardNumber;
        existingCard.Brand = updatedCard.Brand;
        existingCard.CreditLimit = updatedCard.CreditLimit;
        // Do not update existingCard.CreatedAt

        _creditCardRepository.Update(existingCard);
        await _creditCardRepository.SaveChangesAsync();

        return existingCard;
    }
}
