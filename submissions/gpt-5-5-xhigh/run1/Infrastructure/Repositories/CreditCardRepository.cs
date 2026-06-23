using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Repositories;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Repositories;

public sealed class CreditCardRepository(AppDbContext context)
    : RepositoryBase<CreditCard>(context), ICreditCardRepository
{
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        Entities.AsNoTracking().AnyAsync(card => card.Id == id, cancellationToken);
}
