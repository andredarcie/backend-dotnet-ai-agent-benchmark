using CreditCardApi.Application.Common;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Abstractions;

/// <summary>Persistence operations for <see cref="CreditCard"/> aggregates.</summary>
public interface ICreditCardRepository
{
    /// <summary>Loads a card for read-only use (no change tracking), or <see langword="null"/> if it does not exist.</summary>
    Task<CreditCard?> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Loads a card with change tracking enabled so it can be modified and saved.</summary>
    Task<CreditCard?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    /// <summary>Returns one page of cards ordered by id, plus the total count.</summary>
    Task<PagedResult<CreditCard>> GetPageAsync(PaginationQuery page, CancellationToken cancellationToken);

    /// <summary>Checks whether a card with the given id exists.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    /// <summary>Stages a new card for insertion on the next unit-of-work commit.</summary>
    void Add(CreditCard card);

    /// <summary>Deletes a card (and, by cascade, its transactions). Returns <see langword="false"/> if it did not exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
