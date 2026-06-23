using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetCreditCardByIdUseCase(ICreditCardRepository creditCardRepository)
{
    public async Task<CreditCard?> ExecuteAsync(int id)
    {
        return await creditCardRepository.GetByIdAsync(id);
    }
}
