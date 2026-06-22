using CreditCardApi.Application.Common;
using CreditCardApi.Contracts.CreditCards;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.CreditCards;

public sealed class CreateCreditCardUseCase(ICreditCardRepository repository)
{
    public async Task<UseCaseResult<CreditCardResponse>> ExecuteAsync(
        CreditCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = Validation.Validate(request);
        if (errors.Count > 0)
            return UseCaseResult<CreditCardResponse>.Invalid(errors.ToArray());

        var card = new CreditCard
        {
            CardholderName = request.CardholderName!.Trim(),
            CardNumber = request.CardNumber!.Trim(),
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(card, cancellationToken);
        return UseCaseResult<CreditCardResponse>.Success(CreditCardResponse.FromEntity(card));
    }
}
