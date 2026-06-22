using CreditCardApi.Domain.Repositories;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Repositories;

public abstract class RepositoryBase<T>(AppDbContext context) : IRepository<T> where T : class
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<T> Entities { get; } = context.Set<T>();

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Entities.FindAsync([id], cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Entities.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Entities.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
