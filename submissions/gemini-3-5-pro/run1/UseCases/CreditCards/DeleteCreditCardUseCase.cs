using System.Threading.Tasks;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards
{
    public class DeleteCreditCardUseCase
    {
        private readonly ICreditCardRepository _creditCardRepository;

        public DeleteCreditCardUseCase(ICreditCardRepository creditCardRepository)
        {
            _creditCardRepository = creditCardRepository;
        }

        public async Task<UseCaseResult> ExecuteAsync(int id)
        {
            var card = await _creditCardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return UseCaseResult.FailNotFound();
            }

            await _creditCardRepository.DeleteAsync(id);
            return UseCaseResult.Ok();
        }
    }
}
