using System.Threading.Tasks;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards
{
    public class GetCreditCardByIdUseCase
    {
        private readonly ICreditCardRepository _creditCardRepository;

        public GetCreditCardByIdUseCase(ICreditCardRepository creditCardRepository)
        {
            _creditCardRepository = creditCardRepository;
        }

        public async Task<UseCaseResult<CreditCardResponse>> ExecuteAsync(int id)
        {
            var card = await _creditCardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return UseCaseResult<CreditCardResponse>.FailNotFound();
            }

            var response = new CreditCardResponse
            {
                Id = card.Id,
                CardholderName = card.CardholderName,
                CardNumber = card.CardNumber,
                Brand = card.Brand,
                CreditLimit = card.CreditLimit,
                CreatedAt = card.CreatedAt
            };

            return UseCaseResult<CreditCardResponse>.Ok(response);
        }
    }
}
