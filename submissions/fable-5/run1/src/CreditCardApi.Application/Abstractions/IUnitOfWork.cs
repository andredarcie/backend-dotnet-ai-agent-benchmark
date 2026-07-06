namespace CreditCardApi.Application.Abstractions;

/// <summary>Commits the pending changes of the current request as one atomic operation.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists all staged changes in a single database transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="operation"/> inside an explicit database transaction, in a way that is
    /// compatible with the configured retrying execution strategy. Used when several
    /// <see cref="SaveChangesAsync"/> calls must commit or roll back together — e.g. persisting an
    /// entity and its outbox message atomically.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
