using CreditCardApi.Application;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class DeleteCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public DeleteCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> ExecuteAsync(int id, CancellationToken ct = default)
    {
        var card = await _repository.GetByIdAsync(id, ct);
        if (card is null)
        {
            return Result.NotFound();
        }

        await _repository.DeleteAsync(card, ct);
        return Result.Success();
    }
}
