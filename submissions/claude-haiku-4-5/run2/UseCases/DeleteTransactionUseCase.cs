using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class DeleteTransactionUseCase(ITransactionRepository transactionRepository)
{
    public async Task<bool> ExecuteAsync(int id)
    {
        var transaction = await transactionRepository.GetByIdAsync(id);
        if (transaction == null)
            return false;

        await transactionRepository.DeleteAsync(transaction);
        await transactionRepository.SaveChangesAsync();

        return true;
    }
}
