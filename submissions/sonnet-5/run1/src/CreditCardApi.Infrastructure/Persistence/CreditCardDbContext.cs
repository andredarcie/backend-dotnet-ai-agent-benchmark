using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Messaging.Consuming;
using CreditCardApi.Infrastructure.Messaging.Outbox;
using CreditCardApi.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

public class CreditCardDbContext(DbContextOptions<CreditCardDbContext> options) : DbContext(options)
{
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ConsumedTransactionEvent> ConsumedTransactionEvents => Set<ConsumedTransactionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CreditCardConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ConsumedTransactionEventConfiguration());
    }
}
