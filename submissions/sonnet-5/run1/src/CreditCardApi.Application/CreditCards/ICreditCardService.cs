using CreditCardApi.Application.CreditCards.Dtos;
using CreditCardApi.Application.Transactions.Dtos;

namespace CreditCardApi.Application.CreditCards;

public interface ICreditCardService
{
    Task<CreditCardResponse> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken);

    Task<CreditCardResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditCardResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionResponse>> GetTransactionsForCardAsync(int creditCardId, CancellationToken cancellationToken);
}
