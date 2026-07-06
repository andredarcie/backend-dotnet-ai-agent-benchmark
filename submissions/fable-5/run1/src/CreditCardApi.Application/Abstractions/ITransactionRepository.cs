using CreditCardApi.Application.Common;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Abstractions;

/// <summary>Persistence operations for <see cref="Transaction"/> entities.</summary>
public interface ITransactionRepository
{
    /// <summary>Loads a transaction for read-only use (no change tracking), or <see langword="null"/> if it does not exist.</summary>
    Task<Transaction?> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Loads a transaction with change tracking enabled so it can be modified and saved.</summary>
    Task<Transaction?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    /// <summary>Returns one page of all transactions ordered by id, plus the total count.</summary>
    Task<PagedResult<Transaction>> GetPageAsync(PaginationQuery page, CancellationToken cancellationToken);

    /// <summary>Returns one page of the given card's transactions ordered by id, plus the total count.</summary>
    Task<PagedResult<Transaction>> GetPageForCardAsync(int creditCardId, PaginationQuery page, CancellationToken cancellationToken);

    /// <summary>Stages a new transaction for insertion on the next unit-of-work commit.</summary>
    void Add(Transaction transaction);

    /// <summary>Deletes a transaction. Returns <see langword="false"/> if it did not exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
