using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class GetCreditCardByIdUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetCreditCardByIdUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CreditCardResponse>> ExecuteAsync(int id, CancellationToken ct = default)
    {
        var card = await _repository.GetByIdAsync(id, ct);
        return card is null
            ? Result<CreditCardResponse>.NotFound()
            : Result<CreditCardResponse>.Success(card.ToResponse());
    }
}
