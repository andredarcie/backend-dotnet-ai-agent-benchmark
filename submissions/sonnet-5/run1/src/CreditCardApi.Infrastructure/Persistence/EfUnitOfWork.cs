using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

public class EfUnitOfWork(CreditCardDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "The resource was modified by another request in the meantime. Reload it and try again.", ex);
        }
    }
}
