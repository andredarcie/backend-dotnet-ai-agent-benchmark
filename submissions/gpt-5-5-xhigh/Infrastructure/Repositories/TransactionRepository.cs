using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Repositories;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Repositories;

public sealed class TransactionRepository(AppDbContext context)
    : RepositoryBase<Transaction>(context), ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetByCreditCardIdAsync(
        int creditCardId,
        CancellationToken cancellationToken = default) =>
        await Entities
            .AsNoTracking()
            .Where(transaction => transaction.CreditCardId == creditCardId)
            .ToListAsync(cancellationToken);
}
