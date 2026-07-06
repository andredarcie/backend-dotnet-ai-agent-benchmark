using CreditCardApi.Infrastructure.Messaging.Consuming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

internal sealed class ConsumedTransactionEventConfiguration : IEntityTypeConfiguration<ConsumedTransactionEvent>
{
    public void Configure(EntityTypeBuilder<ConsumedTransactionEvent> builder)
    {
        // Natural key: the unique-index violation on a duplicate insert is the dedupe mechanism.
        builder.HasKey(e => e.TransactionId);

        builder.Property(e => e.TransactionId)
            .ValueGeneratedNever();
    }
}
