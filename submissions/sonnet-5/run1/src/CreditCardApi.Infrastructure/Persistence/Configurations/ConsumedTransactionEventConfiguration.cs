using CreditCardApi.Infrastructure.Messaging.Consuming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

public class ConsumedTransactionEventConfiguration : IEntityTypeConfiguration<ConsumedTransactionEvent>
{
    public void Configure(EntityTypeBuilder<ConsumedTransactionEvent> builder)
    {
        builder.ToTable("consumed_transaction_events");
        builder.HasKey(e => e.TransactionId);
        builder.Property(e => e.ConsumedAt).HasColumnType("timestamptz").IsRequired();
    }
}
