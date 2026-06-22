using CreditCardApi.Data;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;
using CreditCardApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories;

public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(AppDbContext context) : base(context) { }

    public async Task<bool> ExistsAsync(int id) =>
        await _dbSet.AnyAsync(c => c.Id == id);
}
