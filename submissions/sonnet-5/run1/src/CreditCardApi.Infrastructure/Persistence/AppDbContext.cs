using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Persistence.Configurations;
using CreditCardApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IPanProtector panProtector)
    : DbContext(options)
{
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CreditCardConfiguration(panProtector));
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
