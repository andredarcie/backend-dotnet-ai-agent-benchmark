using CreditCardApi.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>.</summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    /// <summary>Creates the unit of work over the given context.</summary>
    public UnitOfWork(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        // The execution strategy retries the whole transaction on transient failures (Npgsql resilience).
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
