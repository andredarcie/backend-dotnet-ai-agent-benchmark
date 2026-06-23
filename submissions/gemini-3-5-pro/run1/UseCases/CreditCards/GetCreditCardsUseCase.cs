using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards
{
    public class GetCreditCardsUseCase
    {
        private readonly ICreditCardRepository _creditCardRepository;

        public GetCreditCardsUseCase(ICreditCardRepository creditCardRepository)
        {
            _creditCardRepository = creditCardRepository;
        }

        public async Task<UseCaseResult<IEnumerable<CreditCardResponse>>> ExecuteAsync()
        {
            var cards = await _creditCardRepository.GetAllAsync();
            var response = cards.Select(c => new CreditCardResponse
            {
                Id = c.Id,
                CardholderName = c.CardholderName,
                CardNumber = c.CardNumber,
                Brand = c.Brand,
                CreditLimit = c.CreditLimit,
                CreatedAt = c.CreatedAt
            });

            return UseCaseResult<IEnumerable<CreditCardResponse>>.Ok(response);
        }
    }
}
