namespace CreditCardApi.Application.Abstractions;

/// <summary>
/// Coordinates persistence for a single logical operation. Exposes a transactional scope so the
/// business write and its outbox event commit atomically (Transactional Outbox pattern).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all staged changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction using the provider's
    /// execution strategy (so it is retried as a unit on transient failures), committing on success
    /// and rolling back on error.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
