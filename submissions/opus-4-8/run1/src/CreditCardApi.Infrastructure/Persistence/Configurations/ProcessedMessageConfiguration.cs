using CreditCardApi.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ProcessedMessage"/> (the consumer idempotency ledger).</summary>
public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        // Composite key on (topic, key) makes duplicate handling a primary-key conflict.
        builder.HasKey(m => new { m.Topic, m.MessageKey });
        builder.Property(m => m.Topic).HasMaxLength(200);
        builder.Property(m => m.MessageKey).HasMaxLength(200);
        builder.Property(m => m.ProcessedAt);
    }
}
