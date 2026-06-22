using CreditCardApi.Data;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;
using CreditCardApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId) =>
        await _dbSet
            .Where(t => t.CreditCardId == creditCardId)
            .ToListAsync();
}
