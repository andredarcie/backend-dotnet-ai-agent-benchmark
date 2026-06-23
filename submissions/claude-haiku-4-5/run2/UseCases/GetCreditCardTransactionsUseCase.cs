using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetCreditCardTransactionsUseCase(ICreditCardRepository creditCardRepository)
{
    public async Task<IEnumerable<Transaction>?> ExecuteAsync(int creditCardId)
    {
        var creditCard = await creditCardRepository.GetByIdAsync(creditCardId);
        if (creditCard == null)
            return null;

        return await creditCardRepository.GetTransactionsByCreditCardIdAsync(creditCardId);
    }
}
