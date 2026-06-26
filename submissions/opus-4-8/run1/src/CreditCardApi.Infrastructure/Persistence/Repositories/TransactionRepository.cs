using CreditCardApi.Application.Abstractions;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ITransactionRepository"/>.</summary>
public sealed class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;

    /// <summary>Creates the repository over the given context.</summary>
    public TransactionRepository(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
        await _db.Transactions
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        _db.Transactions.CountAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> ListByCardAsync(int creditCardId, int skip, int take, CancellationToken cancellationToken) =>
        await _db.Transactions
            .AsNoTracking()
            .Where(t => t.CreditCardId == creditCardId)
            .OrderBy(t => t.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountByCardAsync(int creditCardId, CancellationToken cancellationToken) =>
        _db.Transactions.CountAsync(t => t.CreditCardId == creditCardId, cancellationToken);

    /// <inheritdoc />
    public void Add(Transaction transaction) => _db.Transactions.Add(transaction);

    /// <inheritdoc />
    public void Remove(Transaction transaction) => _db.Transactions.Remove(transaction);
}
