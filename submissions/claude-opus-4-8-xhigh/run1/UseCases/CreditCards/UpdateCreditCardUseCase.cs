using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class UpdateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public UpdateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CreditCardResponse>> ExecuteAsync(int id, UpdateCreditCardRequest request, CancellationToken ct = default)
    {
        var card = await _repository.GetByIdAsync(id, ct);
        if (card is null)
        {
            return Result<CreditCardResponse>.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.CardholderName))
        {
            return Result<CreditCardResponse>.Invalid("cardholderName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            return Result<CreditCardResponse>.Invalid("cardNumber is required.");
        }

        if (request.CreditLimit < 0)
        {
            return Result<CreditCardResponse>.Invalid("creditLimit must be greater than or equal to 0.");
        }

        card.CardholderName = request.CardholderName.Trim();
        card.CardNumber = request.CardNumber.Trim();
        card.Brand = request.Brand;
        card.CreditLimit = request.CreditLimit;

        await _repository.UpdateAsync(card, ct);
        return Result<CreditCardResponse>.Success(card.ToResponse());
    }
}
