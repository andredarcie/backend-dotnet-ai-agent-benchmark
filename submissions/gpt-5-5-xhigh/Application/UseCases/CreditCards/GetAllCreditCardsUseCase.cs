using CreditCardApi.Contracts.CreditCards;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.CreditCards;

public sealed class GetAllCreditCardsUseCase(ICreditCardRepository repository)
{
    public async Task<IReadOnlyList<CreditCardResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var cards = await repository.GetAllAsync(cancellationToken);
        return cards.Select(CreditCardResponse.FromEntity).ToList();
    }
}
