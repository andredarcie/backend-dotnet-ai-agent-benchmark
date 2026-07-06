using CreditCardApi.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CreditCardDbContext _db;

    public EfUnitOfWork(CreditCardDbContext db)
    {
        _db = db;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        // A user-initiated transaction must be wrapped in the retrying execution strategy so the
        // whole unit retries as one block on transient failures (EnableRetryOnFailure is on).
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            async ct =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(ct);
                await operation(ct);
                await transaction.CommitAsync(ct);
            },
            cancellationToken);
    }
}
