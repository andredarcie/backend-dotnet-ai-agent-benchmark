using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class DeleteCreditCardUseCase(ICreditCardRepository creditCardRepository)
{
    public async Task<bool> ExecuteAsync(int id)
    {
        var creditCard = await creditCardRepository.GetByIdAsync(id);
        if (creditCard == null)
            return false;

        await creditCardRepository.DeleteAsync(creditCard);
        await creditCardRepository.SaveChangesAsync();

        return true;
    }
}
