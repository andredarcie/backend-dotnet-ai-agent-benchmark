using System.Threading.Tasks;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards
{
    public class UpdateCreditCardUseCase
    {
        private readonly ICreditCardRepository _creditCardRepository;

        public UpdateCreditCardUseCase(ICreditCardRepository creditCardRepository)
        {
            _creditCardRepository = creditCardRepository;
        }

        public async Task<UseCaseResult<CreditCardResponse>> ExecuteAsync(int id, UpdateCreditCardRequest request)
        {
            var card = await _creditCardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return UseCaseResult<CreditCardResponse>.FailNotFound();
            }

            if (string.IsNullOrWhiteSpace(request.CardholderName))
            {
                return UseCaseResult<CreditCardResponse>.Fail("Cardholder name is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.CardNumber))
            {
                return UseCaseResult<CreditCardResponse>.Fail("Card number is required and cannot be empty.");
            }

            if (request.CreditLimit < 0)
            {
                return UseCaseResult<CreditCardResponse>.Fail("Credit limit must be greater than or equal to 0.");
            }

            card.CardholderName = request.CardholderName.Trim();
            card.CardNumber = request.CardNumber.Trim();
            card.Brand = request.Brand?.Trim();
            card.CreditLimit = request.CreditLimit;

            await _creditCardRepository.UpdateAsync(card);

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
