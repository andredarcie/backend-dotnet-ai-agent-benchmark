using System;
using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

public class CreateCreditCardUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;

    public CreateCreditCardUseCase(ICreditCardRepository creditCardRepository)
    {
        _creditCardRepository = creditCardRepository;
    }

    public async Task<CreditCard> ExecuteAsync(CreditCard creditCard)
    {
        // Validation rules
        if (string.IsNullOrWhiteSpace(creditCard.CardholderName) || string.IsNullOrWhiteSpace(creditCard.CardNumber))
        {
            throw new ArgumentException("CardholderName and CardNumber are required.");
        }

        if (creditCard.CreditLimit < 0)
        {
            throw new ArgumentException("CreditLimit must be greater than or equal to 0.");
        }

        creditCard.CreatedAt = DateTime.UtcNow;

        await _creditCardRepository.AddAsync(creditCard);
        await _creditCardRepository.SaveChangesAsync();

        return creditCard;
    }
}
