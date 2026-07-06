using CreditCardApi.Infrastructure.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.Property(m => m.Topic)
            .HasMaxLength(249) // Kafka's topic name limit
            .IsRequired();

        builder.Property(m => m.Key)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.CorrelationId)
            .HasMaxLength(64);

        builder.Property(m => m.LastError)
            .HasMaxLength(2000);

        // Partial index so the dispatcher's "oldest pending first" poll stays cheap
        // no matter how large the processed backlog grows.
        builder.HasIndex(m => m.Id)
            .HasDatabaseName("IX_OutboxMessages_Pending")
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}
