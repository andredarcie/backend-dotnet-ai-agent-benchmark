using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public class CreditCardRepository : ICreditCardRepository
{
    private readonly ApplicationDbContext _context;

    public CreditCardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CreditCard>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<CreditCard> AddAsync(CreditCard creditCard, CancellationToken cancellationToken = default)
    {
        var result = await _context.CreditCards.AddAsync(creditCard, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return result.Entity;
    }

    public async Task UpdateAsync(CreditCard creditCard, CancellationToken cancellationToken = default)
    {
        _context.CreditCards.Update(creditCard);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var card = await _context.CreditCards.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (card != null)
        {
            _context.CreditCards.Remove(card);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
