using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository : ITransactionRepository
{
    private readonly CreditCardDbContext _db;

    public TransactionRepository(CreditCardDbContext db)
    {
        _db = db;
    }

    public Task<Transaction?> GetAsync(int id, CancellationToken cancellationToken) =>
        _db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Transaction?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<PagedResult<Transaction>> GetPageAsync(PaginationQuery page, CancellationToken cancellationToken) =>
        GetPageAsync(_db.Transactions.AsNoTracking(), page, cancellationToken);

    public Task<PagedResult<Transaction>> GetPageForCardAsync(
        int creditCardId,
        PaginationQuery page,
        CancellationToken cancellationToken) =>
        GetPageAsync(
            _db.Transactions.AsNoTracking().Where(t => t.CreditCardId == creditCardId),
            page,
            cancellationToken);

    public void Add(Transaction transaction) => _db.Transactions.Add(transaction);

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _db.Transactions
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    private static async Task<PagedResult<Transaction>> GetPageAsync(
        IQueryable<Transaction> query,
        PaginationQuery page,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Id)
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Transaction>(items, totalCount, page.Page, page.PageSize);
    }
}
