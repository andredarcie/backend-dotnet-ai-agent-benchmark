using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class GetAllCreditCardsUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetAllCreditCardsUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CreditCardResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        var cards = await _repository.GetAllAsync(ct);
        return cards.Select(c => c.ToResponse()).ToList();
    }
}
