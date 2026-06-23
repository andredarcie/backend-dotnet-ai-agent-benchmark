using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetAllTransactionsUseCase(ITransactionRepository transactionRepository)
{
    public async Task<IEnumerable<Transaction>> ExecuteAsync()
    {
        return await transactionRepository.GetAllAsync();
    }
}
