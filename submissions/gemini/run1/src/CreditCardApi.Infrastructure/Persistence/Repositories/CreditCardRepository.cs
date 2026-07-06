using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreditCardApi.Application.Common.Interfaces;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the ICreditCardRepository.
/// </summary>
public class CreditCardRepository : ICreditCardRepository
{
    private readonly AppDbContext _context;

    public CreditCardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // For updates/deletes, we need tracking. For pure GET requests, we can call AsNoTracking.
        // We will default to tracking here, but GET requests can configure AsNoTracking if needed,
        // or we can load with tracking so the entity can be updated immediately.
        return await _context.CreditCards
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CreditCard>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken = default)
    {
        await _context.CreditCards.AddAsync(creditCard, cancellationToken);
    }

    public Task DeleteAsync(CreditCard creditCard, CancellationToken cancellationToken = default)
    {
        _context.CreditCards.Remove(creditCard);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken);
    }
}
