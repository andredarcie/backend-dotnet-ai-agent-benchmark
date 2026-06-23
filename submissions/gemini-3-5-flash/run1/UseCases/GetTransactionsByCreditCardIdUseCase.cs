using System.Collections.Generic;
using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

public class GetTransactionsByCreditCardIdUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;

    public GetTransactionsByCreditCardIdUseCase(ICreditCardRepository creditCardRepository)
    {
        _creditCardRepository = creditCardRepository;
    }

    public async Task<IEnumerable<Transaction>?> ExecuteAsync(int creditCardId)
    {
        // First check if the credit card exists
        var cardExists = await _creditCardRepository.ExistsAsync(creditCardId);
        if (!cardExists)
        {
            return null;
        }

        return await _creditCardRepository.GetTransactionsByCardIdAsync(creditCardId);
    }
}
