using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetTransactionByIdUseCase(ITransactionRepository transactionRepository)
{
    public async Task<Transaction?> ExecuteAsync(int id)
    {
        return await transactionRepository.GetByIdAsync(id);
    }
}
