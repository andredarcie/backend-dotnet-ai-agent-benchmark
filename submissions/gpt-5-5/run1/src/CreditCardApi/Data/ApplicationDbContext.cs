using CreditCardApi.Data.Entities;
using CreditCardApi.Domain.Common;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IncrementConcurrencyVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.ToTable("credit_cards", table =>
            {
                table.HasCheckConstraint("ck_credit_cards_credit_limit_non_negative", "credit_limit >= 0");
            });

            entity.HasKey(card => card.Id);
            entity.Property(card => card.Id).HasColumnName("id");
            entity.Property(card => card.CardholderName).HasColumnName("cardholder_name").HasMaxLength(200).IsRequired();
            entity.Property(card => card.CardNumberCipherText).HasColumnName("card_number_cipher_text").IsRequired();
            entity.Property(card => card.CardNumberLast4).HasColumnName("card_number_last4").HasMaxLength(4).IsRequired();
            entity.Property(card => card.Brand).HasColumnName("brand").HasMaxLength(50);
            entity.Property(card => card.CreditLimit).HasColumnName("credit_limit").HasPrecision(18, 2).IsRequired();
            entity.Property(card => card.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(card => card.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
            entity.HasIndex(card => card.CreatedAt).HasDatabaseName("ix_credit_cards_created_at");
            entity.HasMany(card => card.Transactions)
                .WithOne(transaction => transaction.CreditCard)
                .HasForeignKey(transaction => transaction.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions", table =>
            {
                table.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
            });

            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Id).HasColumnName("id");
            entity.Property(transaction => transaction.CreditCardId).HasColumnName("credit_card_id").IsRequired();
            entity.Property(transaction => transaction.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(transaction => transaction.Merchant).HasColumnName("merchant").HasMaxLength(200).IsRequired();
            entity.Property(transaction => transaction.Category).HasColumnName("category").HasMaxLength(100);
            entity.Property(transaction => transaction.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(transaction => transaction.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
            entity.HasIndex(transaction => transaction.CreditCardId).HasDatabaseName("ix_transactions_credit_card_id");
            entity.HasIndex(transaction => transaction.CreatedAt).HasDatabaseName("ix_transactions_created_at");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(message => message.Topic).HasColumnName("topic").HasMaxLength(200).IsRequired();
            entity.Property(message => message.MessageKey).HasColumnName("message_key").HasMaxLength(200).IsRequired();
            entity.Property(message => message.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(message => message.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(message => message.ProcessedAt).HasColumnName("processed_at");
            entity.Property(message => message.Attempts).HasColumnName("attempts").IsRequired();
            entity.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(2000);
            entity.Property(message => message.DeadLetteredAt).HasColumnName("dead_lettered_at");
            entity.HasIndex(message => new { message.ProcessedAt, message.OccurredAt }).HasDatabaseName("ix_outbox_messages_unprocessed");
            entity.HasIndex(message => message.MessageKey).HasDatabaseName("ix_outbox_messages_message_key");
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.ToTable("processed_messages");
            entity.HasKey(message => message.MessageKey);
            entity.Property(message => message.MessageKey).HasColumnName("message_key").HasMaxLength(200).IsRequired();
            entity.Property(message => message.Topic).HasColumnName("topic").HasMaxLength(200).IsRequired();
            entity.Property(message => message.ProcessedAt).HasColumnName("processed_at").IsRequired();
            entity.HasIndex(message => message.ProcessedAt).HasDatabaseName("ix_processed_messages_processed_at");
        });
    }

    private void IncrementConcurrencyVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyTracked>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Entity.IncrementVersion();
        }
    }
}
