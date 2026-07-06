using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Abstractions;

public interface ICreditCardRepository
{
    /// <summary>Loads a tracked card for mutation (update/delete).</summary>
    Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Loads a card for a read-only response; must not attach it to the change tracker.</summary>
    Task<CreditCard?> FindReadOnlyAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<CreditCard> Items, int TotalCount)> ListReadOnlyAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    void Add(CreditCard creditCard);

    void Remove(CreditCard creditCard);
}
