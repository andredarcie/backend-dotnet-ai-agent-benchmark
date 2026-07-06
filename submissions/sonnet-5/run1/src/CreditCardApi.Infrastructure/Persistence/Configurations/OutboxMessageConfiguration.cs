using CreditCardApi.Infrastructure.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Topic).HasMaxLength(200).IsRequired();
        builder.Property(m => m.CorrelationId).HasMaxLength(100);
        builder.Property(m => m.OccurredAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(m => m.ProcessedAt).HasColumnType("timestamptz");
        builder.Property(m => m.LastError).HasMaxLength(2000);

        builder.HasOne(m => m.Transaction)
            .WithMany()
            .HasForeignKey(m => m.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Partial index: the dispatcher only ever scans pending (unprocessed) rows.
        builder.HasIndex(m => m.OccurredAt)
            .HasDatabaseName("ix_outbox_messages_pending")
            .HasFilter("processed_at IS NULL");
    }
}
