using System.Collections.Generic;
using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

public class GetAllTransactionsUseCase
{
    private readonly ITransactionRepository _transactionRepository;

    public GetAllTransactionsUseCase(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IEnumerable<Transaction>> ExecuteAsync()
    {
        return await _transactionRepository.GetAllAsync();
    }
}
