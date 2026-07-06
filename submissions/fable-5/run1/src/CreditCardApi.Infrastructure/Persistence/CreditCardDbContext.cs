using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Messaging.Consuming;
using CreditCardApi.Infrastructure.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>EF Core context for the credit card domain plus the messaging bookkeeping tables.</summary>
public class CreditCardDbContext : DbContext
{
    /// <summary>Creates the context with externally configured options.</summary>
    public CreditCardDbContext(DbContextOptions<CreditCardDbContext> options)
        : base(options)
    {
    }

    /// <summary>Credit cards.</summary>
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    /// <summary>Transactions charged against credit cards.</summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>Transactional-outbox rows awaiting delivery to Kafka.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Idempotency ledger of Kafka events that have already been consumed.</summary>
    public DbSet<ConsumedTransactionEvent> ConsumedTransactionEvents => Set<ConsumedTransactionEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CreditCardDbContext).Assembly);
}
