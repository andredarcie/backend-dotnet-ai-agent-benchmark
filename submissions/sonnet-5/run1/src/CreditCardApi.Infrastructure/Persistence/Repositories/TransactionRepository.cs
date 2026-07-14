using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository(AppDbContext dbContext) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetByCreditCardIdAsync(int creditCardId, CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.CreditCardId == creditCardId)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

    public void Add(Transaction transaction) => dbContext.Transactions.Add(transaction);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
