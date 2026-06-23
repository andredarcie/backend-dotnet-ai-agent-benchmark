using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class GetCreditCardTransactionsUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetCreditCardTransactionsUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TransactionResponse>>> ExecuteAsync(int creditCardId, CancellationToken ct = default)
    {
        var transactions = await _repository.GetTransactionsAsync(creditCardId, ct);
        if (transactions is null)
        {
            return Result<IReadOnlyList<TransactionResponse>>.NotFound();
        }

        IReadOnlyList<TransactionResponse> payload = transactions.Select(t => t.ToResponse()).ToList();
        return Result<IReadOnlyList<TransactionResponse>>.Success(payload);
    }
}
