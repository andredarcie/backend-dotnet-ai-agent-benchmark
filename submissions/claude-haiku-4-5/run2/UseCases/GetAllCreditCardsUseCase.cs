using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetAllCreditCardsUseCase(ICreditCardRepository creditCardRepository)
{
    public async Task<IEnumerable<CreditCard>> ExecuteAsync()
    {
        return await creditCardRepository.GetAllAsync();
    }
}
