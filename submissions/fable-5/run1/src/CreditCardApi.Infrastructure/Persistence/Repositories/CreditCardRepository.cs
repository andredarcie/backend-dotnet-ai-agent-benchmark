using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

internal sealed class CreditCardRepository : ICreditCardRepository
{
    private readonly CreditCardDbContext _db;

    public CreditCardRepository(CreditCardDbContext db)
    {
        _db = db;
    }

    public Task<CreditCard?> GetAsync(int id, CancellationToken cancellationToken) =>
        _db.CreditCards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CreditCard?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _db.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<CreditCard>> GetPageAsync(PaginationQuery page, CancellationToken cancellationToken)
    {
        var query = _db.CreditCards.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Id)
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CreditCard>(items, totalCount, page.Page, page.PageSize);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        _db.CreditCards.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);

    public void Add(CreditCard card) => _db.CreditCards.Add(card);

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        // Single-statement delete; the FK cascade removes the card's transactions in the database.
        var deleted = await _db.CreditCards
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
