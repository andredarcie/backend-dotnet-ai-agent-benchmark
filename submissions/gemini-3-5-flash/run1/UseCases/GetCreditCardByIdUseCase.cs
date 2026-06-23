using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

public class GetCreditCardByIdUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;

    public GetCreditCardByIdUseCase(ICreditCardRepository creditCardRepository)
    {
        _creditCardRepository = creditCardRepository;
    }

    public async Task<CreditCard?> ExecuteAsync(int id)
    {
        return await _creditCardRepository.GetByIdAsync(id);
    }
}
