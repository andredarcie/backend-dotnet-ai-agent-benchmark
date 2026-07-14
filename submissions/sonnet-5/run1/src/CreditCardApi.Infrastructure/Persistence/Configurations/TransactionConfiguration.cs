using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasColumnType("numeric(19,2)");

        builder.Property(t => t.Merchant)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Category)
            .HasMaxLength(100);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Explicit alongside the FK-driven convention index: this is the column every
        // "transactions for card X" / lookup-by-id query filters on.
        builder.HasIndex(t => t.CreditCardId);
        builder.HasIndex(t => t.CreatedAt);
    }
}
