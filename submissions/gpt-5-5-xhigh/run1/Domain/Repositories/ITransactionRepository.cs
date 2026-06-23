using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Domain.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IReadOnlyList<Transaction>> GetByCreditCardIdAsync(
        int creditCardId,
        CancellationToken cancellationToken = default);
}
