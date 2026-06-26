using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Abstractions;

/// <summary>Read/write access to <see cref="Transaction"/> records.</summary>
public interface ITransactionRepository
{
    /// <summary>Loads a transaction for update (tracked), or <c>null</c> if it does not exist.</summary>
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Returns one page of transactions ordered by id, as a read-only (no-tracking) projection.</summary>
    Task<IReadOnlyList<Transaction>> ListAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>Total number of transactions.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>Returns one page of transactions for a single card, ordered by id (no-tracking).</summary>
    Task<IReadOnlyList<Transaction>> ListByCardAsync(int creditCardId, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Number of transactions belonging to a single card.</summary>
    Task<int> CountByCardAsync(int creditCardId, CancellationToken cancellationToken);

    /// <summary>Stages a new transaction for insertion on the next unit-of-work save.</summary>
    void Add(Transaction transaction);

    /// <summary>Stages a transaction for deletion on the next unit-of-work save.</summary>
    void Remove(Transaction transaction);
}
