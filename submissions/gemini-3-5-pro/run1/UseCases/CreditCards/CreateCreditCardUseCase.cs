using System;
using System.Threading.Tasks;
using CreditCardApi.Domain;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards
{
    public class CreateCreditCardUseCase
    {
        private readonly ICreditCardRepository _creditCardRepository;

        public CreateCreditCardUseCase(ICreditCardRepository creditCardRepository)
        {
            _creditCardRepository = creditCardRepository;
        }

        public async Task<UseCaseResult<CreditCardResponse>> ExecuteAsync(CreateCreditCardRequest request)
        {
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

            var card = new CreditCard
            {
                CardholderName = request.CardholderName.Trim(),
                CardNumber = request.CardNumber.Trim(),
                Brand = request.Brand?.Trim(),
                CreditLimit = request.CreditLimit,
                CreatedAt = DateTime.UtcNow
            };

            await _creditCardRepository.AddAsync(card);

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
