using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Persistence.Inbox;
using CreditCardApi.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>The EF Core unit of work / database session for the API.</summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>Creates the context with the supplied options.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>Credit cards.</summary>
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    /// <summary>Transactions.</summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>Pending integration events (Transactional Outbox).</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Keys of messages already consumed (idempotency ledger).</summary>
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
