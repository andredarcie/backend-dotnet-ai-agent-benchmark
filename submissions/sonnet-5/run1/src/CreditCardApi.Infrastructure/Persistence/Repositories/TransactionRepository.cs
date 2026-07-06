using CreditCardApi.Application.Abstractions;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

public class TransactionRepository(CreditCardDbContext dbContext) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Transaction?> FindReadOnlyAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> ListReadOnlyAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions.AsNoTracking().OrderBy(t => t.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> ListByCreditCardReadOnlyAsync(
        int creditCardId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions.AsNoTracking()
            .Where(t => t.CreditCardId == creditCardId)
            .OrderBy(t => t.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public void Add(Transaction transaction) => dbContext.Transactions.Add(transaction);

    public void Remove(Transaction transaction) => dbContext.Transactions.Remove(transaction);
}
