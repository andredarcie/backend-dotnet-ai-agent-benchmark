using System.Threading.Tasks;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions
{
    public class DeleteTransactionUseCase
    {
        private readonly ITransactionRepository _transactionRepository;

        public DeleteTransactionUseCase(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<UseCaseResult> ExecuteAsync(int id)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return UseCaseResult.FailNotFound();
            }

            await _transactionRepository.DeleteAsync(id);
            return UseCaseResult.Ok();
        }
    }
}
