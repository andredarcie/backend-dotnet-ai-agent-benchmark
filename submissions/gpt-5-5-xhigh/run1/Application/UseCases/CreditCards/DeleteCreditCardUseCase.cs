using CreditCardApi.Application.Common;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.CreditCards;

public sealed class DeleteCreditCardUseCase(ICreditCardRepository repository)
{
    public async Task<UseCaseResult<bool>> ExecuteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var card = await repository.GetByIdAsync(id, cancellationToken);
        if (card is null)
            return UseCaseResult<bool>.NotFound();

        await repository.DeleteAsync(card, cancellationToken);
        return UseCaseResult<bool>.Success(true);
    }
}
