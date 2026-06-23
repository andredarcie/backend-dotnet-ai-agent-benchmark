using CreditCardApi.Data;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories;

public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(AppDbContext context) : base(context) { }

    public async Task<CreditCard?> GetByIdWithTransactionsAsync(int id)
        => await Context.CreditCards
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);
}
