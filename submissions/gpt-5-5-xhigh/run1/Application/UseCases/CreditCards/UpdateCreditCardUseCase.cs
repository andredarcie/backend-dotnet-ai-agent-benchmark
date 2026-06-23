using CreditCardApi.Application.Common;
using CreditCardApi.Contracts.CreditCards;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.CreditCards;

public sealed class UpdateCreditCardUseCase(ICreditCardRepository repository)
{
    public async Task<UseCaseResult<CreditCardResponse>> ExecuteAsync(
        int id,
        CreditCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = Validation.Validate(request);
        if (errors.Count > 0)
            return UseCaseResult<CreditCardResponse>.Invalid(errors.ToArray());

        var card = await repository.GetByIdAsync(id, cancellationToken);
        if (card is null)
            return UseCaseResult<CreditCardResponse>.NotFound();

        card.CardholderName = request.CardholderName!.Trim();
        card.CardNumber = request.CardNumber!.Trim();
        card.Brand = request.Brand;
        card.CreditLimit = request.CreditLimit;

        await repository.UpdateAsync(card, cancellationToken);
        return UseCaseResult<CreditCardResponse>.Success(CreditCardResponse.FromEntity(card));
    }
}
