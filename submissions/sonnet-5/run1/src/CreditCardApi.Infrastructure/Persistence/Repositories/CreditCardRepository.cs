using CreditCardApi.Application.CreditCards;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

public sealed class CreditCardRepository(AppDbContext dbContext) : ICreditCardRepository
{
    public Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.CreditCards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CreditCard>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        await dbContext.CreditCards
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        dbContext.CreditCards.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);

    public void Add(CreditCard creditCard) => dbContext.CreditCards.Add(creditCard);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
