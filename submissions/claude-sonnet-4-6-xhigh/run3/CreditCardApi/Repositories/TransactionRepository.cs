using CreditCardApi.Data;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Transaction>> GetByCardIdAsync(int creditCardId)
        => await Context.Transactions
            .Where(t => t.CreditCardId == creditCardId)
            .ToListAsync();
}
