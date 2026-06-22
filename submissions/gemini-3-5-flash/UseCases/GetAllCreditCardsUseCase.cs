using System.Collections.Generic;
using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

public class GetAllCreditCardsUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;

    public GetAllCreditCardsUseCase(ICreditCardRepository creditCardRepository)
    {
        _creditCardRepository = creditCardRepository;
    }

    public async Task<IEnumerable<CreditCard>> ExecuteAsync()
    {
        return await _creditCardRepository.GetAllAsync();
    }
}
