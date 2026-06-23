using CreditCardApi.Models;

namespace CreditCardApi.Repositories;

public interface ICreditCardRepository : IRepository<CreditCard>
{
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns the transactions for a card, or <c>null</c> if the card does not exist.
    /// </summary>
    Task<List<Transaction>?> GetTransactionsAsync(int creditCardId, CancellationToken ct = default);
}
