using System.Threading.Tasks;
using Gemini.Data.Repositories;

namespace Gemini.UseCases;

public class DeleteTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;

    public DeleteTransactionUseCase(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<bool> ExecuteAsync(int id)
    {
        var existingTransaction = await _transactionRepository.GetByIdAsync(id);
        if (existingTransaction == null)
        {
            return false;
        }

        _transactionRepository.Delete(existingTransaction);
        await _transactionRepository.SaveChangesAsync();

        return true;
    }
}
