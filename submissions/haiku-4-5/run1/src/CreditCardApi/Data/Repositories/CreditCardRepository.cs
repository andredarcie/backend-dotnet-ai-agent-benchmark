using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public class CreditCardRepository(ApplicationDbContext context) : ICreditCardRepository
{
    public async Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.CreditCards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CreditCard>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await context.CreditCards
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<CreditCard> CreateAsync(CreditCard creditCard, CancellationToken cancellationToken = default)
    {
        context.CreditCards.Add(creditCard);
        await context.SaveChangesAsync(cancellationToken);
        return creditCard;
    }

    public async Task<CreditCard?> UpdateAsync(int id, CreditCard updatedCard, CancellationToken cancellationToken = default)
    {
        var existingCard = await context.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (existingCard == null)
        {
            return null;
        }

        existingCard.CardholderName = updatedCard.CardholderName;
        existingCard.CardNumber = updatedCard.CardNumber;
        existingCard.Brand = updatedCard.Brand;
        existingCard.CreditLimit = updatedCard.CreditLimit;

        await context.SaveChangesAsync(cancellationToken);
        return existingCard;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var card = await context.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (card == null)
        {
            return false;
        }

        context.CreditCards.Remove(card);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
