using CreditCardApi.Contracts.CreditCards;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.CreditCards;

public sealed class GetCreditCardByIdUseCase(ICreditCardRepository repository)
{
    public async Task<CreditCardResponse?> ExecuteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var card = await repository.GetByIdAsync(id, cancellationToken);
        return card is null ? null : CreditCardResponse.FromEntity(card);
    }
}
