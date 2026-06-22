using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class CreateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public CreateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CreditCardResponse>> ExecuteAsync(CreateCreditCardRequest request, CancellationToken ct = default)
    {
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

        var card = new CreditCard
        {
            CardholderName = request.CardholderName.Trim(),
            CardNumber = request.CardNumber.Trim(),
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(card, ct);
        return Result<CreditCardResponse>.Success(created.ToResponse());
    }
}
