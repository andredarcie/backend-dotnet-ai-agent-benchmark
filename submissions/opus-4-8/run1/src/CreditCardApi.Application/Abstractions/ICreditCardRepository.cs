using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Abstractions;

/// <summary>Read/write access to <see cref="CreditCard"/> aggregates.</summary>
public interface ICreditCardRepository
{
    /// <summary>Loads a card for update (tracked), or <c>null</c> if it does not exist.</summary>
    Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Returns whether a card with the given id exists, without materializing it.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    /// <summary>Returns one page of cards ordered by id, as a read-only (no-tracking) projection.</summary>
    Task<IReadOnlyList<CreditCard>> ListAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>Total number of cards.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new card for insertion on the next unit-of-work save.</summary>
    void Add(CreditCard card);

    /// <summary>Stages a card for deletion on the next unit-of-work save.</summary>
    void Remove(CreditCard card);
}
