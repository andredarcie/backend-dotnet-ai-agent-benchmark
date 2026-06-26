using CreditCardApi.Application.Abstractions;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICreditCardRepository"/>.</summary>
public sealed class CreditCardRepository : ICreditCardRepository
{
    private readonly AppDbContext _db;

    /// <summary>Creates the repository over the given context.</summary>
    public CreditCardRepository(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _db.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        _db.CreditCards.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CreditCard>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
        await _db.CreditCards
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        _db.CreditCards.CountAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(CreditCard card) => _db.CreditCards.Add(card);

    /// <inheritdoc />
    public void Remove(CreditCard card) => _db.CreditCards.Remove(card);
}
