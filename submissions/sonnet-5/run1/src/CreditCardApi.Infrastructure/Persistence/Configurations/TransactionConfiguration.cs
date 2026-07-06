using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(t => t.Merchant).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(100);
        builder.Property(t => t.CreatedAt).HasColumnType("timestamptz").IsRequired();

        builder.Property<uint>("xmin").IsRowVersion();

        // Redundant with the convention EF applies for the FK, kept explicit as it's the exact
        // access pattern the read paths (list-by-card, FK existence checks) rely on.
        builder.HasIndex(t => t.CreditCardId);
    }
}
