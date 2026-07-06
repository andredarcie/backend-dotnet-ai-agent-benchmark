using CreditCardApi.Application.Abstractions;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

public class CreditCardRepository(CreditCardDbContext dbContext) : ICreditCardRepository
{
    public Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CreditCard?> FindReadOnlyAsync(int id, CancellationToken cancellationToken) =>
        dbContext.CreditCards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        dbContext.CreditCards.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<CreditCard> Items, int TotalCount)> ListReadOnlyAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.CreditCards.AsNoTracking().OrderBy(c => c.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public void Add(CreditCard creditCard) => dbContext.CreditCards.Add(creditCard);

    public void Remove(CreditCard creditCard) => dbContext.CreditCards.Remove(creditCard);
}
