using CreditCardApi.Data;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories;

public class CreditCardRepository(AppDbContext context) : RepositoryBase<CreditCard>(context), ICreditCardRepository
{
    public async Task<IEnumerable<Transaction>> GetTransactionsByCreditCardIdAsync(int creditCardId)
    {
        return await Context.Transactions
            .Where(t => t.CreditCardId == creditCardId)
            .ToListAsync();
    }
}
