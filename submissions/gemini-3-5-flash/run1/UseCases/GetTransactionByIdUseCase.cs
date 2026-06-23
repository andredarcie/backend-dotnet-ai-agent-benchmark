using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

public class GetTransactionByIdUseCase
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByIdUseCase(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Transaction?> ExecuteAsync(int id)
    {
        return await _transactionRepository.GetByIdAsync(id);
    }
}
