using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CreditCardApi.Application.Common.Interfaces;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Credit Card REST API.
/// </summary>
public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly ICardEncryptionService _encryptionService;

    public DbSet<CreditCard> CreditCards { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<ProcessedConsumerMessage> ProcessedConsumerMessages { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICardEncryptionService encryptionService)
        : base(options)
    {
        _encryptionService = encryptionService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Value Converter for CreditCard Number encryption
        var encryptionConverter = new ValueConverter<string, string>(
            plainText => _encryptionService.Encrypt(plainText),
            cipherText => _encryptionService.Decrypt(cipherText)
        );

        // CreditCard Entity Configuration
        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.ToTable("credit_cards");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            entity.Property(c => c.CardholderName).HasColumnName("cardholder_name").IsRequired();
            entity.Property(c => c.CardNumber)
                  .HasColumnName("card_number")
                  .IsRequired()
                  .HasConversion(encryptionConverter);
            entity.Property(c => c.Brand).HasColumnName("brand");
            entity.Property(c => c.CreditLimit).HasColumnName("credit_limit").HasPrecision(18, 2);
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            
            // PostgreSQL optimistic concurrency token via xmin column
            entity.Property(c => c.RowVersion)
                  .HasColumnName("xmin")
                  .HasColumnType("xid")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsConcurrencyToken();
        });

        // Transaction Entity Configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            entity.Property(t => t.CreditCardId).HasColumnName("credit_card_id");
            entity.Property(t => t.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entity.Property(t => t.Merchant).HasColumnName("merchant").IsRequired();
            entity.Property(t => t.Category).HasColumnName("category");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");

            // Foreign Key and Indexes
            entity.HasOne(t => t.CreditCard)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(t => t.CreditCardId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.CreditCardId).HasDatabaseName("ix_transactions_credit_card_id");
        });

        // OutboxMessage Entity Configuration
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).HasColumnName("id");
            entity.Property(o => o.Type).HasColumnName("type").IsRequired();
            entity.Property(o => o.Content).HasColumnName("content").IsRequired();
            entity.Property(o => o.OccurredOnUtc).HasColumnName("occurred_on_utc").HasColumnType("timestamp with time zone");
            entity.Property(o => o.ProcessedOnUtc).HasColumnName("processed_on_utc").HasColumnType("timestamp with time zone");
            entity.Property(o => o.Error).HasColumnName("error");

            entity.HasIndex(o => o.ProcessedOnUtc).HasDatabaseName("ix_outbox_messages_processed_on_utc");
        });

        // ProcessedConsumerMessage Entity Configuration
        modelBuilder.Entity<ProcessedConsumerMessage>(entity =>
        {
            entity.ToTable("processed_consumer_messages");
            entity.HasKey(p => p.MessageId);
            entity.Property(p => p.MessageId).HasColumnName("message_id");
            entity.Property(p => p.ProcessedAtUtc).HasColumnName("processed_at_utc").HasColumnType("timestamp with time zone");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Intercept and find added transactions to generate OutboxMessages
        var addedTxns = ChangeTracker.Entries<Transaction>()
            .Where(e => e.State == EntityState.Added)
            .ToList();

        if (addedTxns.Count == 0)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        // We have added transactions. We must run this inside a transaction to get their generated IDs
        // and commit both the Transactions and the OutboxMessages atomically.
        var isOuterTransaction = Database.CurrentTransaction != null;
        var transaction = isOuterTransaction ? null : await Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Save changes to write transactions and generate their IDs
            var result = await base.SaveChangesAsync(cancellationToken);

            // 2. Generate OutboxMessages now that IDs are generated
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            foreach (var entry in addedTxns)
            {
                var txn = entry.Entity;
                var payload = JsonSerializer.Serialize(new
                {
                    id = txn.Id,
                    creditCardId = txn.CreditCardId,
                    amount = txn.Amount,
                    merchant = txn.Merchant,
                    category = txn.Category,
                    createdAt = txn.CreatedAt
                }, options);

                var outboxMessage = new OutboxMessage(
                    Guid.NewGuid(),
                    "TransactionCreated",
                    payload
                );

                OutboxMessages.Add(outboxMessage);
            }

            // 3. Save changes again to write the OutboxMessages
            await base.SaveChangesAsync(cancellationToken);

            // 4. Commit transaction
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
