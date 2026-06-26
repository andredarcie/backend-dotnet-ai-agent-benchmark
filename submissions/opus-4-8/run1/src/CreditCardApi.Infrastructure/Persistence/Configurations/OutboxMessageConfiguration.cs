using CreditCardApi.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="OutboxMessage"/>.</summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Topic).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Key).IsRequired().HasMaxLength(200);
        builder.Property(m => m.EventType).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.CreatedAt);
        builder.Property(m => m.ProcessedAt);
        builder.Property(m => m.Attempts);
        builder.Property(m => m.LastError);

        // The dispatcher polls for unprocessed rows oldest-first.
        builder.HasIndex(m => new { m.ProcessedAt, m.CreatedAt });
    }
}
